using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.Service.BackgroundMediaPlayer;

public interface IBackgroundMediaPlayerService
{
    ValueTask UpdateMediaPlayerElementAsync(MediaPlayerElement element, CancellationToken token = default);

    void Pause();

    void Play();

    void Stop();

    void SetSessionLocked(bool value);

    void SetWindowVisible(bool value);
}
