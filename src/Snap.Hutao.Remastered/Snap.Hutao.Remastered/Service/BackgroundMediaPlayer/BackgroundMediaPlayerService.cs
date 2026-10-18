using Microsoft.UI.Xaml.Controls;
using System.IO;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Caching;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Web.Hoyolab.HoyoPlay;
using Windows.Media.Core;
using Windows.Media.Playback;
using System.Collections.Frozen;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Service.BackgroundMediaPlayer;

[Service(ServiceLifetime.Singleton, typeof(IBackgroundMediaPlayerService))]
public sealed partial class BackgroundMediaPlayerService : IBackgroundMediaPlayerService
{
    private static readonly HashSet<string> AllowedVideoFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".webm", ".m4v", ".mov", ".wmv", ".avi"
    };

    private static readonly FrozenDictionary<string, string> videoUriMapper = new Dictionary<string, string>()
    {
        { "https://launcher-webstatic.mihoyo.com/launcher-public/2026/08/05/8e1c78aaa6e33ed60b88e12a461f8ee5_6759941859929004894.webm", "https://static.snaphutaorp.org/static/launcher/20260812.mp4" }
    }.ToFrozenDictionary();

    private readonly AppOptions appOptions;
    private readonly ILogger<BackgroundMediaPlayerService> logger;
    private readonly IServiceProvider serviceProvider;

    // Owned by this service and borrowed by MediaPlayerElement. Detach the
    // element before Stop closes the player; only the UI thread accesses it.
    private MediaPlayer? mediaPlayer;
    private BackgroundMediaPlaybackController? playbackController;

    private BackgroundMediaPlaybackController PlaybackController => playbackController ??= new(ApplyPlaybackState);

    [GeneratedConstructor]
    public partial BackgroundMediaPlayerService(IServiceProvider serviceProvider);

    public void Pause()
    {
        PlaybackController.SetPlaybackRequested(false);
    }

    public void Play()
    {
        PlaybackController.SetPlaybackRequested(true);
    }

    public void SetSessionLocked(bool value) => PlaybackController.SetSessionLocked(value);

    public void SetWindowVisible(bool value) => PlaybackController.SetWindowVisible(value);

    private void ApplyPlaybackState(bool shouldPlay)
    {
        if (shouldPlay) mediaPlayer?.Play();
        else mediaPlayer?.Pause();
    }

    public void Stop()
    {
        MediaPlayer? player = Interlocked.Exchange(ref mediaPlayer, null);
        if (player is null)
        {
            return;
        }

        try
        {
            player.Dispose();
        }
        catch (Exception ex) when (ex is COMException or ObjectDisposedException)
        {
            // Nothing left to release: the player is already closed.
            logger.LogWarning(ex, "Failed to dispose the background media player");
        }
    }

    public async ValueTask UpdateMediaPlayerElementAsync(MediaPlayerElement element, CancellationToken token = default)
    {
        if (element is null)
        {
            return;
        }

        ITaskContext taskContext = TaskContext.GetForDependencyObject(element);

        await taskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();
        if (!PlaybackController.IsWindowVisible) return;

        // Only the playback controller may start the player. AutoPlay could
        // otherwise resume a late source load after the workstation was locked.
        element.AutoPlay = false;
        element.AreTransportControlsEnabled = false;

        // Own the player explicitly. A MediaPlayerElement-created player may be
        // closed by the framework before Unloaded calls Stop on that same object.
        MediaPlayer player = mediaPlayer ??= new MediaPlayer();
        player.AutoPlay = false;
        player.IsMuted = appOptions.IsBackgroundMediaMuted.Value;
        player.IsLoopingEnabled = appOptions.IsBackgroundMediaLooping.Value;
        element.SetMediaPlayer(player);

        Uri? videoUri = appOptions.BackgroundMediaType.Value switch
        {
            BackgroundMediaType.LocalFolder => await GetLocalFolderVideoUriAsync(token).ConfigureAwait(false),
            BackgroundMediaType.OfficialLauncher => await GetOfficialLauncherVideoUriAsync(token).ConfigureAwait(false),
            _ => null,
        };

        await taskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();

        if (!ReferenceEquals(mediaPlayer, player)) return;
        player.Source = videoUri is null ? null : MediaSource.CreateFromUri(videoUri);

        // Wallpaper playback must not appear in system transport controls or
        // respond to media keys; keep this on the explicitly owned player.
        DisableSystemMediaTransportControls(player);
        PlaybackController.Apply();
    }

    private async ValueTask<Uri?> GetLocalFolderVideoUriAsync(CancellationToken token)
    {
        string folder = string.IsNullOrEmpty(appOptions.BackgroundMediaPath.Value) ? HutaoRuntime.GetDataBackgroundDirectory() : appOptions.BackgroundMediaPath.Value!;

        string? selected;
        try
        {
            // Recursively enumerating a user selected folder (possibly a network share) is never
            // cheap enough for the UI thread, and it happens on every refresh.
            selected = await Task.Run(
                () =>
                {
                    if (!Directory.Exists(folder))
                    {
                        return null;
                    }

                    return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                        .Where(static path => AllowedVideoFormats.Contains(Path.GetExtension(path)))
                        .OrderBy(static _ => Guid.NewGuid())
                        .FirstOrDefault();
                },
                token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to enumerate the background media folder '{Folder}'", folder);
            return null;
        }

        if (selected is null)
        {
            return null;
        }

        return new Uri(Path.GetFullPath(selected));
    }

    private async ValueTask<Uri?> GetOfficialLauncherVideoUriAsync(CancellationToken token)
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            OfficialLauncherClient? launcherClient = scope.ServiceProvider.GetService<OfficialLauncherClient>();
            if (launcherClient is null)
            {
                return null;
            }

            try
            {
                string? videoUrl = await launcherClient.GetBackgroundVideoUrlAsync(token).ConfigureAwait(false);
                if (string.IsNullOrEmpty(videoUrl))
                {
                    return null;
                }

                if (videoUriMapper.TryGetValue(videoUrl, out string? mappedUrl))
                {
                    videoUrl = mappedUrl;
                }

                Uri targetUri = new(videoUrl);

                IImageCache? imageCache = scope.ServiceProvider.GetService<IImageCache>();
                if (imageCache is null)
                {
                    return targetUri;
                }

                return await TryGetCachedVideoUriAsync(imageCache, targetUri).ConfigureAwait(false) ?? targetUri;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve the official launcher background video");
                return null;
            }
        }
    }

    private async ValueTask<Uri?> TryGetCachedVideoUriAsync(IImageCache imageCache, Uri targetUri)
    {
        try
        {
            ValueFile file = await imageCache.GetFileFromCacheAsync(targetUri).ConfigureAwait(false);

            string filePath = file.ToString();
            string cacheDir = HutaoRuntime.GetLocalCacheImageCacheDirectory();
            if (Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(cacheDir), StringComparison.OrdinalIgnoreCase) && File.Exists(filePath))
            {
                return new Uri(filePath);
            }

            try
            {
                imageCache.Remove(targetUri);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to remove the invalid cached background video '{Uri}'", targetUri);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache the background video '{Uri}', streaming it instead", targetUri);
        }

        return null;
    }

    private static void DisableSystemMediaTransportControls(MediaPlayer? player)
    {
        if (player is null)
        {
            return;
        }

        player.CommandManager.IsEnabled = false;
    }
}
