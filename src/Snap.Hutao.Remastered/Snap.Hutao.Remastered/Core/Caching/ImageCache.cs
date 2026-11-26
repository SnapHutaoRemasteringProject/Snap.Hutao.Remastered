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
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Core.Caching;

[Service(ServiceLifetime.Singleton, typeof(IImageCache))]
public sealed partial class ImageCache : IImageCache
{
    private static readonly FrozenSet<string> SupportedSchemes =
    [
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
    ];

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

    public ValueFile? TryGetExistingFile(Uri uri)
    {
        Debug.Assert(SupportedSchemes.Contains(uri.Scheme), "Unsupported URI scheme");

        // 与 GetSourceFileAsync 的查找顺序一致，只是不做下载那一步：
        // 先看可写缓存，再看安装包内自带的资源包。
        ImageCacheFile cache = ImageCacheFile.Create(CacheFolder, uri);
        if (CachedImageResource.TryRead(cache.DefaultFilePath) is { } cached)
        {
            return cached.File;
        }

        return GetBundledFile(uri) is { } bundledPath && CachedImageResource.TryRead(bundledPath) is { } bundled
            ? bundled.File
            : null;
    }

    public async ValueTask<ValueFile> GetFileFromCacheAsync(Uri uri, ElementTheme theme)
    {
        return (await GetImageAsync(uri, theme).ConfigureAwait(false)).File;
    }

    public async ValueTask<CachedImageResource> GetImageAsync(Uri uri, ElementTheme theme, CachedImageResource? failedResource = null)
    {
        Debug.Assert(SupportedSchemes.Contains(uri.Scheme), "Unsupported URI scheme");

        ImageCacheFile cache = ImageCacheFile.Create(CacheFolder, uri);
        using (await downloadLocks.LockAsync(cache.HashedFileName).ConfigureAwait(false))
        {
            CachedImageResource source = await GetSourceFileAsync(uri, cache, failedResource).ConfigureAwait(false);
            if (theme == ElementTheme.Default)
            {
                return source;
            }

            string themedPath = cache.GetThemedFile(theme);
            if (CachedImageResource.TryRead(themedPath) is { } themed && themed != failedResource
                && themed.LastWriteTimeUtc >= source.LastWriteTimeUtc)
            {
                return themed;
            }

            try
            {
                return await CreateThemedMonochromeFileAsync(source.File, themedPath, theme).ConfigureAwait(false);
            }
            catch (COMException ex) when (IsInvalidImage(ex.HResult))
            {
                // The monochrome converter decodes the cached source before WinUI sees it.
                source = await GetSourceFileAsync(uri, cache, source).ConfigureAwait(false);
                return await CreateThemedMonochromeFileAsync(source.File, themedPath, theme).ConfigureAwait(false);
            }
        }
    }

    public ValueFile GetFileFromCategoryAndName(string category, string fileName)
    {
        return ImageCacheFile.Create(CacheFolder, StaticResourcesEndpoints.StaticRaw(category, fileName)).DefaultFilePath;
    }

    private async ValueTask<CachedImageResource> GetSourceFileAsync(Uri uri, ImageCacheFile cache, CachedImageResource? failedResource)
    {
        if (CachedImageResource.TryRead(cache.DefaultFilePath) is { } cached)
        {
            if (cached != failedResource)
            {
                return cached;
            }

            File.Delete(cache.DefaultFilePath);
        }

        if (GetBundledFile(uri) is { } bundledPath && CachedImageResource.TryRead(bundledPath) is { } bundled && bundled != failedResource)
        {
            return bundled;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateInfo("Begin to download file", "Core.Caching.ImageCache", [("Uri", uri.ToString()), ("File", cache.DefaultFilePath)]));
        await downloadOperation.DownloadFileAsync(uri, cache.DefaultFilePath).ConfigureAwait(false);
        return CachedImageResource.TryRead(cache.DefaultFilePath)
            ?? throw InternalImageCacheException.Throw($"'{uri.OriginalString}': Image file is empty after download.");
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

    private static bool IsInvalidImage(int hresult)
    {
        return unchecked((uint)hresult) is 0x88982F07 // WINCODEC_ERR_UNKNOWNIMAGEFORMAT
            or 0x88982F50 // WINCODEC_ERR_COMPONENTNOTFOUND
            or 0x88982F60 // WINCODEC_ERR_BADIMAGE
            or 0x88982F61 // WINCODEC_ERR_BADHEADER
            or 0x88982F63; // WINCODEC_ERR_BADSTREAMDATA
    }

    private static async ValueTask<CachedImageResource> CreateThemedMonochromeFileAsync(ValueFile sourceFile, string themedPath, ElementTheme theme)
    {
        string temporary = themedPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using FileStream sourceStream = File.OpenRead(sourceFile);
            using (FileStream themeStream = File.Create(temporary))
            {
                await MonoChromeImageConverter.ConvertAndCopyToAsync(theme, sourceStream, themeStream).ConfigureAwait(false);
            }
            File.Move(temporary, themedPath, true);
            return CachedImageResource.TryRead(themedPath)
                ?? throw InternalImageCacheException.Throw($"'{themedPath}': Converted image is empty.");
        }
        catch (IOException ex)
        {
            throw InternalImageCacheException.Throw("Failed to convert image", ex);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}

