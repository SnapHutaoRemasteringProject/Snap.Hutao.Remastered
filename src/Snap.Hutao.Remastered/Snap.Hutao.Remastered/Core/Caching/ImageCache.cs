// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
using Snap.Hutao.Remastered.Win32;
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using ThemeFile = (Microsoft.UI.Xaml.ElementTheme, Snap.Hutao.Remastered.Core.IO.ValueFile);

namespace Snap.Hutao.Remastered.Core.Caching;

[Service(ServiceLifetime.Singleton, typeof(IImageCache))]
public sealed partial class ImageCache : IImageCache
{
    private static readonly FrozenSet<string> SupportedSchemes =
    [
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
    ];

    private readonly AsyncKeyedLock<ThemeFile> themeFileLocks = new();
    private readonly AsyncKeyedLock<string> downloadLocks = new();

    private readonly IImageCacheDownloadOperation downloadOperation;
    private readonly BundledImageResources bundledImages;

    [GeneratedConstructor]
    public partial ImageCache(IServiceProvider serviceProvider);

    private string CacheFolder
    {
        get => LazyInitializer.EnsureInitialized(ref field, static () =>
        {
            try
            {
                string folder = HutaoRuntime.GetLocalCacheImageCacheDirectory();
                Directory.CreateDirectory(Path.Combine(folder, "Light"));
                Directory.CreateDirectory(Path.Combine(folder, "Dark"));
                return folder;
            }
            catch (Exception ex)
            {
                // 0x80070570 ERROR_FILE_CORRUPT
                HutaoNative.Instance.ShowErrorMessage(ex.Message, ExceptionFormat.Format(ex));
                ProcessFactory.KillCurrent();
                return string.Empty;
            }
        });
    }

    public void Remove(Uri uriForCachedItem)
    {
        try
        {
            File.Delete(ImageCacheFile.GetHashedFile(CacheFolder, uriForCachedItem));
        }
        catch
        {
            // Ignored
        }
    }

    public ValueTask<ValueFile> GetFileFromCacheAsync(Uri uri)
    {
        return GetFileFromCacheAsync(uri, ElementTheme.Default);
    }

    public async ValueTask<ValueFile> GetFileFromCacheAsync(Uri uri, ElementTheme theme)
    {
        Debug.Assert(SupportedSchemes.Contains(uri.Scheme), "Unsupported URI scheme");

        ImageCacheFile imageCacheFile = ImageCacheFile.Create(CacheFolder, uri);
        string themedFileFullPath = imageCacheFile.GetThemedFile(theme);

        using (await themeFileLocks.LockAsync((theme, imageCacheFile.HashedFileName)).ConfigureAwait(false))
        {
            string sourcePath = imageCacheFile.DefaultFilePath;
            if (IsFileInvalid(themedFileFullPath) || IsFileInvalid(imageCacheFile.DefaultFilePath))
            {
                using (await downloadLocks.LockAsync(imageCacheFile.HashedFileName).ConfigureAwait(false))
                {
                    // Preserve existing cache overrides (including newer snapshots and other qualities).
                    // Only resolve this application's static-image endpoint to the installed snapshot.
                    string? bundledPath = GetBundledFile(uri);
                    if (IsFileInvalid(imageCacheFile.DefaultFilePath))
                    {
                        if (bundledPath is not null)
                        {
                            sourcePath = bundledPath;
                        }
                        else
                        {
                            SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateInfo("Begin to download file", "Core.Caching.ImageCache", [("Uri", uri.ToString()), ("File", imageCacheFile.DefaultFilePath)]));

                            try
                            {
                                await downloadOperation.DownloadFileAsync(uri, imageCacheFile.DefaultFilePath).ConfigureAwait(false);
                            }
                            catch (Exception)
                            {
                                Remove(uri);
                                throw;
                            }
                        }
                    }
                }
            }

            // Default-theme images are read in place. Only derived monochrome images need writable storage.
            if (theme is ElementTheme.Default)
            {
                return sourcePath;
            }

            await EnsureThemedMonochromeFileExistsAsync(imageCacheFile, sourcePath, theme).ConfigureAwait(false);
            return themedFileFullPath;
        }
    }

    public ValueFile GetFileFromCategoryAndName(string category, string fileName)
    {
        return ImageCacheFile.Create(CacheFolder, StaticResourcesEndpoints.StaticRaw(category, fileName)).DefaultFilePath;
    }

    private static bool IsFileInvalid(string file, bool treatNullFileAsInvalid = true)
    {
        if (!File.Exists(file))
        {
            return treatNullFileAsInvalid;
        }

        return new FileInfo(file).Length == 0;
    }

    private string? GetBundledFile(Uri uri)
    {
        Uri root = new(StaticResourcesEndpoints.Root.TrimEnd('/') + "/static/raw/");
        if (!root.IsBaseOf(uri) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return null;
        }

        return bundledImages.Bundle.Find(Uri.UnescapeDataString(root.MakeRelativeUri(uri).ToString()));
    }

    public async ValueTask StoreFileAsync(string category, string fileName, Stream source, CancellationToken token = default)
    {
        if (!ImageResourceBundle.IsResourcePath($"{category}/{fileName}"))
        {
            throw new InvalidDataException("Invalid static image archive entry.");
        }

        ImageCacheFile cache = ImageCacheFile.Create(CacheFolder, StaticResourcesEndpoints.StaticRaw(category, fileName));
        using (await downloadLocks.LockAsync(cache.HashedFileName).ConfigureAwait(false))
        {
            string temporary = cache.DefaultFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream output = File.Create(temporary))
                {
                    await source.CopyToAsync(output, token).ConfigureAwait(false);
                }

                if (new FileInfo(temporary).Length == 0)
                {
                    throw new InvalidDataException("Empty static image archive entry.");
                }

                // The archive may be newer than the installed snapshot: only skip identical bytes.
                if (await bundledImages.Bundle.FindIdenticalAsync(temporary, $"{category}/{fileName}", token).ConfigureAwait(false) is not null)
                {
                    if (await bundledImages.Bundle.FindIdenticalAsync(cache.DefaultFilePath, $"{category}/{fileName}", token).ConfigureAwait(false) is not null)
                    {
                        File.Delete(cache.DefaultFilePath);
                    }
                }
                else
                {
                    File.Move(temporary, cache.DefaultFilePath, true);
                }
            }
            finally
            {
                File.Delete(temporary);
            }
        }
    }

    public async ValueTask<(int Files, long Bytes)> RemoveDuplicateFilesAsync(CancellationToken token = default)
    {
        int files = 0;
        long bytes = 0;
        if (!bundledImages.IsAvailable)
        {
            return (files, bytes);
        }

        // Preserve the existing URL-hash layout and recognize both current and previous official hosts.
        string[] roots = [StaticResourcesEndpoints.Root, "https://api.snaphutaorp.org", "https://api.hutaorp.org", "https://static.snaphutaorp.org", "https://static.hutaorp.org"];
        foreach (string resourcePath in bundledImages.Bundle.ResourcePaths)
        {
            foreach (string root in roots.Distinct(StringComparer.Ordinal))
            {
                token.ThrowIfCancellationRequested();
                string name = ImageCacheFile.GetHashedFileName($"{root.TrimEnd('/')}/static/raw/{resourcePath}");
                string path = Path.Combine(CacheFolder, name);

                using (await downloadLocks.LockAsync(name).ConfigureAwait(false))
                {
                    try
                    {
                        if (await bundledImages.Bundle.FindIdenticalAsync(path, resourcePath, token).ConfigureAwait(false) is not null)
                        {
                            long length = new FileInfo(path).Length;
                            File.Delete(path);
                            files++;
                            bytes += length;
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        SentrySdk.CaptureException(ex);
                    }
                }
            }
        }

        return (files, bytes);
    }

    private static async ValueTask EnsureThemedMonochromeFileExistsAsync(ImageCacheFile imageCacheFile, string sourcePath, ElementTheme theme)
    {
        if (theme is ElementTheme.Default)
        {
            return;
        }

        try
        {
            using (FileStream sourceStream = File.OpenRead(sourcePath))
            {
                using (FileStream themeStream = File.Create(imageCacheFile.GetThemedFile(theme)))
                {
                    await MonoChromeImageConverter.ConvertAndCopyToAsync(theme, sourceStream, themeStream).ConfigureAwait(false);
                }
            }
        }
        catch (IOException ex)
        {
            throw InternalImageCacheException.Throw("Failed to convert image", ex);
        }
    }
}

