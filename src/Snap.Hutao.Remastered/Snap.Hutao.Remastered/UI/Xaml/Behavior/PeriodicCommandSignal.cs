// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

public sealed partial class PeriodicCommandSignal(TimeSpan period) : IDisposable
{
    private readonly object gate = new();
    private readonly PeriodicTimer timer = new(period);
    private TaskCompletionSource themeChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task<bool>? nextTick;
    private bool stopped;

    public void NotifyThemeChanged()
    {
        lock (gate)
        {
            if (!stopped) themeChanged.TrySetResult();
        }
    }

    public async Task<bool> WaitAsync()
    {
        Task<bool> tick;
        Task theme;
        lock (gate)
        {
            if (stopped) return false;
            tick = nextTick ??= timer.WaitForNextTickAsync().AsTask();
            theme = themeChanged.Task;
        }

        await Task.WhenAny(tick, theme).ConfigureAwait(false);
        lock (gate)
        {
            if (stopped) return false;
            if (theme.IsCompleted) themeChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!tick.IsCompleted) return true;
            nextTick = null;
            return tick.GetAwaiter().GetResult();
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (stopped) return;
            stopped = true;
            // Dispose completes a pending timer wait with false, not a thrown
            // cancellation. No token source can be recreated after detaching.
            timer.Dispose();
            themeChanged.TrySetResult();
        }
    }
}
