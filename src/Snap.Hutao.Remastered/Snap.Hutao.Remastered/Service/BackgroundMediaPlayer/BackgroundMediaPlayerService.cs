using Microsoft.UI.Xaml.Controls;
using System.IO;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Caching;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Web.Hoyolab.HoyoPlay;
using Windows.Media.Core;
using Windows.Media.Playback;
using System.Collections.Frozen;

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
    private readonly IServiceProvider serviceProvider;

    private MediaPlayer? mediaPlayer;

    [GeneratedConstructor]
    public partial BackgroundMediaPlayerService(IServiceProvider serviceProvider);

    public void Pause()
    {
        mediaPlayer?.Pause();
    }

    public void Play()
    {
        mediaPlayer?.Play();
    }

    public void Stop()
    {
        if (mediaPlayer is not null)
        {
            mediaPlayer.Source = null;
            mediaPlayer = null;
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

        element.AutoPlay = true;
        element.AreTransportControlsEnabled = false;

        if (element.MediaPlayer is not null)
        {
            element.MediaPlayer.IsMuted = appOptions.IsBackgroundMediaMuted.Value;
            element.MediaPlayer.IsLoopingEnabled = appOptions.IsBackgroundMediaLooping.Value;
        }

        mediaPlayer = element.MediaPlayer;

        switch (appOptions.BackgroundMediaType.Value)
        {
            case BackgroundMediaType.LocalFolder:
                string folder = string.IsNullOrEmpty(appOptions.BackgroundMediaPath.Value) ? HutaoRuntime.GetDataBackgroundDirectory() : appOptions.BackgroundMediaPath.Value!;

                if (!Directory.Exists(folder))
                {
                    element.Source = null;
                    return;
                }

                IEnumerable<string> files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                    .Where(p => AllowedVideoFormats.Contains(Path.GetExtension(p)));

                string? selected = files.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();

                if (selected is null)
                {
                    element.Source = null;
                    return;
                }

                // Ensure local file path uses file:// scheme for MediaSource
                element.Source = MediaSource.CreateFromUri(new Uri(Path.GetFullPath(selected)));
                break;

            case BackgroundMediaType.OfficialLauncher:
                using (IServiceScope scope = serviceProvider.CreateScope())
                {
                    OfficialLauncherClient? launcherClient = scope.ServiceProvider.GetService<OfficialLauncherClient>();
                    if (launcherClient is not null)
                    {
                        try
                        {
                            string? videoUrl = await launcherClient.GetBackgroundVideoUrlAsync(token).ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(videoUrl))
                            {
                                IImageCache? imageCache = scope.ServiceProvider.GetService<IImageCache>();
                                if (videoUriMapper.TryGetValue(videoUrl, out string? mappedUrl))
                                {
                                    videoUrl = mappedUrl;
                                }

                                Uri targetUri = new(videoUrl);

                                if (imageCache is not null)
                                {
                                    try
                                    {
                                        ValueFile file = await imageCache.GetFileFromCacheAsync(targetUri).ConfigureAwait(false);

                                        string filePath = file.ToString();
                                        string cacheDir = HutaoRuntime.GetLocalCacheImageCacheDirectory();
                                        if (Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(cacheDir), StringComparison.OrdinalIgnoreCase) && File.Exists(filePath))
                                        {
                                            await taskContext.SwitchToMainThreadAsync();
                                            element.Source = MediaSource.CreateFromUri(new Uri(filePath));
                                            break;
                                        }

                                        try
                                        {
                                            imageCache.Remove(targetUri);
                                        }
                                        catch
                                        {
                                            // ignore remove failure
                                        }
                                    }
                                    catch
                                    {
                                        // ignore cache error, fallback to streaming
                                    }
                                }

                                element.Source = MediaSource.CreateFromUri(targetUri);
                                break;
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    element.Source = null;
                }

                break;

            case BackgroundMediaType.None:
            default:
                // Clear source
                element.Source = null;
                break;
        }
    }
}
