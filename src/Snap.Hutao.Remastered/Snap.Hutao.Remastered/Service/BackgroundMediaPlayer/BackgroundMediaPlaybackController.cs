// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.BackgroundMediaPlayer;

/// <summary>Combines independent reasons to suspend wallpaper playback.</summary>
public sealed class BackgroundMediaPlaybackController
{
    private readonly Action<bool> apply;
    private bool playbackRequested = true;
    private bool sessionLocked;

    public BackgroundMediaPlaybackController(Action<bool> apply)
    {
        this.apply = apply;
    }

    public bool IsWindowVisible { get; private set; }

    public void SetWindowVisible(bool value)
    {
        if (IsWindowVisible == value) return;
        IsWindowVisible = value;
        Apply();
    }

    public void SetSessionLocked(bool value)
    {
        if (sessionLocked == value) return;
        sessionLocked = value;
        Apply();
    }

    public void SetPlaybackRequested(bool value)
    {
        playbackRequested = value;
        Apply();
    }

    // Source loading is asynchronous. Recheck the current conditions when it
    // completes rather than remembering whether playback was allowed at its start.
    public void Apply()
    {
        apply(playbackRequested && IsWindowVisible && !sessionLocked);
    }
}
