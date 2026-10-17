// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

public static class FlyoutOpenStateBinding
{
    private static readonly ConditionalWeakTable<Flyout, PendingSwitch> States = new();

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.RegisterAttached(
        "IsOpen", typeof(bool), typeof(FlyoutOpenStateBinding), new PropertyMetadata(false));
    public static readonly DependencyProperty SwitchDelayMillisecondsProperty = DependencyProperty.RegisterAttached(
        "SwitchDelayMilliseconds", typeof(int), typeof(FlyoutOpenStateBinding), new PropertyMetadata(120));
    public static readonly DependencyProperty IsTrackingEnabledProperty = DependencyProperty.RegisterAttached(
        "IsTrackingEnabled", typeof(bool), typeof(FlyoutOpenStateBinding), new PropertyMetadata(false, OnIsTrackingEnabledChanged));

    public static bool GetIsOpen(Flyout obj) => (bool)obj.GetValue(IsOpenProperty);
    public static void SetIsOpen(Flyout obj, bool value) => obj.SetValue(IsOpenProperty, value);
    public static int GetSwitchDelayMilliseconds(Flyout obj) => (int)obj.GetValue(SwitchDelayMillisecondsProperty);
    public static void SetSwitchDelayMilliseconds(Flyout obj, int value) => obj.SetValue(SwitchDelayMillisecondsProperty, value);
    public static bool GetIsTrackingEnabled(Flyout obj) => (bool)obj.GetValue(IsTrackingEnabledProperty);
    public static void SetIsTrackingEnabled(Flyout obj, bool value) => obj.SetValue(IsTrackingEnabledProperty, value);

    public sealed class PendingSwitch
    {
        public object Gate { get; } = new();
        public CancellationTokenSource? Pending { get; set; }
        public bool Tracking { get; set; }
    }

    public static void CancelPendingSwitches()
    {
        foreach (KeyValuePair<Flyout, PendingSwitch> pair in States)
            Stop(pair.Value);
    }

    private static void Stop(PendingSwitch state)
    {
        lock (state.Gate)
        {
            state.Tracking = false;
            state.Pending?.Cancel();
        }
    }

    private static void OnIsTrackingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Flyout flyout || XamlApplicationLifetime.Exiting) return;
        PendingSwitch state = States.GetValue(flyout, static _ => new());
        flyout.Opened -= OnFlyoutOpened;
        flyout.Closed -= OnFlyoutClosed;
        if ((bool)e.NewValue)
        {
            lock (state.Gate) state.Tracking = true;
            flyout.Opened += OnFlyoutOpened;
            flyout.Closed += OnFlyoutClosed;
        }
        else Stop(state);
    }

    private static void OnFlyoutOpened(object? sender, object e)
    {
        if (sender is Flyout flyout) Schedule(flyout, true);
    }

    private static void OnFlyoutClosed(object? sender, object e)
    {
        if (sender is Flyout flyout) Schedule(flyout, false);
    }

    private static void Schedule(Flyout flyout, bool value)
    {
        if (XamlApplicationLifetime.Exiting || !States.TryGetValue(flyout, out PendingSwitch? state)) return;
        try
        {
            // Capture all XAML values on the UI thread. The delayed worker and its
            // finally block only use managed state, never DependencyObject.GetValue.
            int delay = Math.Max(0, GetSwitchDelayMilliseconds(flyout));
            DispatcherQueue dispatcher = flyout.DispatcherQueue;
            CancellationTokenSource source;
            lock (state.Gate)
            {
                if (!state.Tracking) return;
                state.Pending?.Cancel();
                source = new();
                state.Pending = source;
            }
            RunAsync(new WeakReference<Flyout>(flyout), dispatcher, state, source, value, delay).SafeForget();
        }
        catch (COMException exception) when (IsClosed(exception)) { Stop(state); }
    }

    private static bool IsClosed(COMException exception) => exception.HResult is unchecked((int)0x80000013)
        or unchecked((int)0x8000FFFF) or unchecked((int)0x80004004) or unchecked((int)0x80010108);

    private static async Task RunAsync(WeakReference<Flyout> weak, DispatcherQueue dispatcher, PendingSwitch state, CancellationTokenSource source, bool value, int delay)
    {
        CancellationToken token = source.Token;
        try
        {
            await Task.Delay(delay, token).ConfigureAwait(false);
            if (token.IsCancellationRequested || XamlApplicationLifetime.Exiting) return;
            TaskCompletionSource applied = new(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!dispatcher.TryEnqueue(() =>
            {
                try
                {
                    lock (state.Gate)
                    {
                        if (state.Tracking && ReferenceEquals(state.Pending, source) && !token.IsCancellationRequested
                            && !XamlApplicationLifetime.Exiting && weak.TryGetTarget(out Flyout? target))
                            SetIsOpen(target, value);
                    }
                    applied.TrySetResult();
                }
                catch (COMException exception) when (IsClosed(exception)) { applied.TrySetResult(); }
                catch (Exception exception) { applied.TrySetException(exception); }
            })) return;
            await applied.Task.WaitAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        finally
        {
            lock (state.Gate)
                if (ReferenceEquals(state.Pending, source)) state.Pending = null;
            source.Dispose();
        }
    }
}
