// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.UI.Xaml.Media.Animation;

/// <summary>An element-owned transition whose callbacks are removed before unloading.</summary>
public static class OpacityTransition
{
    public static Task StartAsync(FrameworkElement element, double opacity, TimeSpan duration, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (XamlApplicationLifetime.Exiting || !element.IsLoaded) return Task.CompletedTask;

        Storyboard storyboard = new();
        DoubleAnimation animation = new() { To = opacity, Duration = new Duration(duration) };
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, nameof(UIElement.Opacity));
        storyboard.Children.Add(animation);

        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        EventHandler<object>? completed = null;
        RoutedEventHandler? unloaded = null;
        bool finished = false;

        void Finish(bool canceled)
        {
            if (finished) return;
            finished = true;
            // Unsubscribe before stopping, not when an animation's native callback
            // eventually arrives after its island/dispatcher has been destroyed.
            storyboard.Completed -= completed;
            element.Unloaded -= unloaded;
            registration.Dispose();
            try
            {
                if (!canceled) element.Opacity = opacity;
                storyboard.Stop();
            }
            catch (COMException exception) when (canceled && exception.HResult is unchecked((int)0x80004004) or unchecked((int)0x80000013))
            {
                // Stop can race the framework's own animation teardown. Our
                // handlers are already removed; a closed animation needs no more work.
                System.Diagnostics.Trace.WriteLine($"Opacity transition already closed: {exception.HResult:X8}");
            }
            if (canceled) completion.TrySetCanceled();
            else completion.TrySetResult();
        }

        completed = (_, _) => Finish(false);
        unloaded = (_, _) => Finish(true);
        storyboard.Completed += completed;
        element.Unloaded += unloaded;
        Microsoft.UI.Dispatching.DispatcherQueue dispatcher = element.DispatcherQueue;
        registration = token.Register(() =>
        {
            if (dispatcher.HasThreadAccess) Finish(true);
            else if (!dispatcher.TryEnqueue(() => Finish(true))) completion.TrySetCanceled();
        });

        if (!finished) storyboard.Begin();
        else registration.Dispose();
        return completion.Task;
    }
}
