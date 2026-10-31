// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using JetBrains.Annotations;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Caching;
using Snap.Hutao.Remastered.Core.Graphics.Imaging;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.UI;
using Snap.Hutao.Remastered.Web.Hutao.Wallpaper;
using Snap.Hutao.Remastered.Web.Response;
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics.Imaging;

namespace Snap.Hutao.Remastered.Service.BackgroundImage;

[Service(ServiceLifetime.Singleton, typeof(IBackgroundImageService))]
public sealed partial class BackgroundImageService : IBackgroundImageService
{
    private static readonly FrozenSet<string> AllowedFormats = [".bmp", ".gif", ".ico", ".jpg", ".jpeg", ".png", ".tiff", ".webp"];

    private readonly BackgroundImageOptions backgroundImageOptions;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;
    private readonly AppOptions appOptions;
    private readonly ILogger<BackgroundImageService> logger;
    private readonly IMessenger messenger;

    private HashSet<string>? availableBackgroundPathSet;
    private string? currentBackgroundFolder;

    private int decodeFailureHintShown;

    [GeneratedConstructor]
    public partial BackgroundImageService(IServiceProvider serviceProvider);

    public async ValueTask<ValueResult</* shouldRefresh */ bool, BackgroundImage?>> GetNextBackgroundImageAsync(BackgroundImage? previous, CancellationToken token = default)
    {
        // backgroundImageOptions.Wallpaper will also be set in this method if web wallpaper type is selected
        HashSet<string> availableBackgroundSet = await SkipOrInitAvailableBackgroundAsync(previous, token).ConfigureAwait(false);

        // The availableBackgroundSet will be empty if BackgroundImageType is None
        if (availableBackgroundSet.Count <= 0)
        {
            return new(previous is not null, default!);
        }

        string path = System.Random.Shared.GetItems([.. availableBackgroundSet], 1)[0];
        availableBackgroundSet.Remove(path);

        if (string.Equals(path, previous?.Path, StringComparison.OrdinalIgnoreCase))
        {
            return new(false, default!);
        }

        if (!File.Exists(path))
        {
            Debugger.Break();
            return new(false, default!);
        }

        FileStream fileStream;
        try
        {
            fileStream = File.OpenRead(path);
        }
        catch (IOException)
        {
            return new(false, default!);
        }

        using (fileStream)
        {
            Rgba32 accentColor;
            try
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream.AsRandomAccessStream());
                using (SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight))
                {
                    accentColor = softwareBitmap.GetRgba32AccentColor();
                }
            }
            catch (COMException comException)
            {
                logger.LogWarning(comException, "Failed to decode the background image '{Path}' (type={Type}, HResult=0x{HResult:X8})", path, appOptions.BackgroundImageType.Value, comException.HResult);

                if (appOptions.BackgroundImageType.Value is not BackgroundImageType.LocalFolder)
                {
                    NotifyDecodeFailureOnce(comException);
                    return new(false, default!);
                }

                // 0xC00D36BE MF_E_INVALID_FILE_FORMAT: Throw to let user know which file is invalid
                // E_FAIL
                if (comException.HResult is not unchecked((int)0x80004005))
                {
                    comException.Data.Add("FilePath", path);
                    comException.Data.Add("HResult", $"0x{comException.HResult:X8}");
                    throw;
                }

                return new(false, default!);
            }

            await taskContext.SwitchToMainThreadAsync();

            BackgroundImage background = new()
            {
                Path = path,
                AccentColor = accentColor,
                Luminance = accentColor.Luminance,
            };

            return new(true, background);
        }
    }

    private async ValueTask<HashSet<string>> SkipOrInitAvailableBackgroundAsync(BackgroundImage? previous, CancellationToken token = default)
    {
        switch (appOptions.BackgroundImageType.Value)
        {
            case BackgroundImageType.LocalFolder:
                {
                    string backgroundFolder = string.IsNullOrEmpty(appOptions.BackgroundImagePath.Value)
                        ? HutaoRuntime.GetDataBackgroundDirectory()
                        : appOptions.BackgroundImagePath.Value;

                    // Invalidate cache if folder changed
                    if (!string.Equals(backgroundFolder, currentBackgroundFolder, StringComparison.Ordinal))
                    {
                        availableBackgroundPathSet = null;
                        currentBackgroundFolder = backgroundFolder;
                    }

                    if (availableBackgroundPathSet is not { Count: > 0 })
                    {
                        IEnumerable<string> imageFiles = Directory
                            .EnumerateFiles(backgroundFolder, "*", SearchOption.AllDirectories)
                            .Where(path => AllowedFormats.Contains(Path.GetExtension(path)));

                        IEnumerable<string> shortcutTargets = Directory
                            .EnumerateFiles(backgroundFolder, "*.lnk", SearchOption.AllDirectories)
                            .Select(FileSystem.ResolveLink)
                            .Where(target => target is not null)
                            .SelectMany(target =>
                                Directory.Exists(target!)
                                    ? Directory.EnumerateFiles(target!, "*", SearchOption.TopDirectoryOnly)
                                        .Where(p => AllowedFormats.Contains(Path.GetExtension(p)))
                                    : AllowedFormats.Contains(Path.GetExtension(target!)) ? (string[])[target!] : []);

                        availableBackgroundPathSet = [.. imageFiles.Concat(shortcutTargets)];

                        // Why > 1: If only one file in the folder, don't change background
                        if (previous is not null && availableBackgroundPathSet.Count > 1)
                        {
                            availableBackgroundPathSet.Remove(previous.Path);
                        }
                    }

                    await taskContext.SwitchToMainThreadAsync();
                    backgroundImageOptions.Wallpaper = default;
                    break;
                }

            case BackgroundImageType.HutaoBing:
                await SetCurrentBackgroundPathSetAsync(static (client, token) => client.GetBingWallpaperAsync(token), token).ConfigureAwait(false);
                break;
            case BackgroundImageType.HutaoDaily:
                await SetCurrentBackgroundPathSetAsync(static (client, token) => client.GetTodayWallpaperAsync(token), token).ConfigureAwait(false);
                break;
            case BackgroundImageType.HutaoOfficialLauncher:
                await SetCurrentBackgroundPathSetAsync(static (client, token) => client.GetLauncherWallpaperAsync(token), token).ConfigureAwait(false);
                break;
            default:
                availableBackgroundPathSet = [];
                break;
        }

        return availableBackgroundPathSet ??= [];
    }

    private async ValueTask SetCurrentBackgroundPathSetAsync([RequireStaticDelegate] Func<HutaoWallpaperClient, CancellationToken, ValueTask<Response<Wallpaper>>> responseFactory, CancellationToken token = default)
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            HutaoWallpaperClient wallpaperClient = scope.ServiceProvider.GetRequiredService<HutaoWallpaperClient>();
            Response<Wallpaper> response = await responseFactory(wallpaperClient, token).ConfigureAwait(false);
            if (response is { Data: { } wallpaper })
            {
                if (wallpaper.Url is { } url)
                {
                    try
                    {
                        ValueFile file = await scope.ServiceProvider.GetRequiredService<IImageCache>().GetFileFromCacheAsync(url).ConfigureAwait(false);
                        availableBackgroundPathSet = [file];
                    }
                    catch (InternalImageCacheException ex)
                    {
                        logger.LogWarning(ex, "Failed to download the background image '{Url}'", url);
                        availableBackgroundPathSet = [];
                    }
                }

                await taskContext.SwitchToMainThreadAsync();
                backgroundImageOptions.Wallpaper = wallpaper;
            }
        }
    }

    // 0x88982F8B WINCODEC_ERR_COMPONENTINITIALIZEFAILURE and 0x88982F50 WINCODEC_ERR_COMPONENTNOTFOUND both
    // mean that this machine has no usable codec for the image format: tell the user once instead of silently
    // keeping the previous wallpaper (the web wallpaper are provided by us, users cannot fix the file).
    private void NotifyDecodeFailureOnce(COMException comException)
    {
        if (comException.HResult is not (unchecked((int)0x88982F8B) or unchecked((int)0x88982F50)))
        {
            return;
        }

        if (Interlocked.Exchange(ref decodeFailureHintShown, 1) is 1)
        {
            return;
        }

        messenger.Send(InfoBarMessage.Warning(SH.ServiceBackgroundImageDecodeFailed));
    }
}

