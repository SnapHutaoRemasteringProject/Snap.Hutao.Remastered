// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

public static class FlyoutOpenStateBinding
{
    public static bool GetIsOpen(Flyout obj)
    {
        return (bool)obj.GetValue(IsOpenProperty);
    }

    public static void SetIsOpen(Flyout obj, bool value)
    {
        obj.SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.RegisterAttached(
        "IsOpen",
        typeof(bool),
        typeof(FlyoutOpenStateBinding),
        new PropertyMetadata(false));

    public static int GetSwitchDelayMilliseconds(Flyout obj)
    {
        return (int)obj.GetValue(SwitchDelayMillisecondsProperty);
    }

    public static void SetSwitchDelayMilliseconds(Flyout obj, int value)
    {
        obj.SetValue(SwitchDelayMillisecondsProperty, value);
    }

    public static readonly DependencyProperty SwitchDelayMillisecondsProperty = DependencyProperty.RegisterAttached(
        "SwitchDelayMilliseconds",
        typeof(int),
        typeof(FlyoutOpenStateBinding),
        new PropertyMetadata(120));

    public static bool GetIsTrackingEnabled(Flyout obj)
    {
        return (bool)obj.GetValue(IsTrackingEnabledProperty);
    }

    public static void SetIsTrackingEnabled(Flyout obj, bool value)
    {
        obj.SetValue(IsTrackingEnabledProperty, value);
    }

    public static readonly DependencyProperty IsTrackingEnabledProperty = DependencyProperty.RegisterAttached(
        "IsTrackingEnabled",
        typeof(bool),
        typeof(FlyoutOpenStateBinding),
        new PropertyMetadata(false, OnIsTrackingEnabledChanged));

    private static readonly DependencyProperty IsHookedProperty = DependencyProperty.RegisterAttached(
        "IsHooked",
        typeof(bool),
        typeof(FlyoutOpenStateBinding),
        new PropertyMetadata(false));

    private static readonly DependencyProperty PendingSwitchTokenSourceProperty = DependencyProperty.RegisterAttached(
        "PendingSwitchTokenSource",
        typeof(CancellationTokenSource),
        typeof(FlyoutOpenStateBinding),
        new PropertyMetadata(null));

    private static bool GetIsHooked(Flyout obj)
    {
        return (bool)obj.GetValue(IsHookedProperty);
    }

    private static void SetIsHooked(Flyout obj, bool value)
    {
        obj.SetValue(IsHookedProperty, value);
    }

    private static CancellationTokenSource? GetPendingSwitchTokenSource(Flyout obj)
    {
        return (CancellationTokenSource?)obj.GetValue(PendingSwitchTokenSourceProperty);
    }

    private static void SetPendingSwitchTokenSource(Flyout obj, CancellationTokenSource? value)
    {
        obj.SetValue(PendingSwitchTokenSourceProperty, value);
    }

    private static void OnIsTrackingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Flyout flyout)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            if (!GetIsHooked(flyout))
            {
                flyout.Opened += OnFlyoutOpened;
                flyout.Closed += OnFlyoutClosed;
                SetIsHooked(flyout, true);
            }

            return;
        }

        if (GetIsHooked(flyout))
        {
            flyout.Opened -= OnFlyoutOpened;
            flyout.Closed -= OnFlyoutClosed;
            SetIsHooked(flyout, false);
        }

        GetPendingSwitchTokenSource(flyout)?.Cancel();
        GetPendingSwitchTokenSource(flyout)?.Dispose();
        SetPendingSwitchTokenSource(flyout, null);
    }

    private static void OnFlyoutOpened(object? sender, object e)
    {
        if (sender is Flyout flyout)
        {
            _ = SetIsOpenDelayedAsync(flyout, true);
        }
    }

    private static void OnFlyoutClosed(object? sender, object e)
    {
        if (sender is Flyout flyout)
        {
            _ = SetIsOpenDelayedAsync(flyout, false);
        }
    }

    private static async ValueTask SetIsOpenDelayedAsync(Flyout flyout, bool value)
    {
        GetPendingSwitchTokenSource(flyout)?.Cancel();
        GetPendingSwitchTokenSource(flyout)?.Dispose();

        CancellationTokenSource tokenSource = new();
        SetPendingSwitchTokenSource(flyout, tokenSource);

        int delay = Math.Max(0, GetSwitchDelayMilliseconds(flyout));

        try
        {
            if (delay > 0)
            {
                await Task.Delay(delay, tokenSource.Token).ConfigureAwait(false);
            }

            if (tokenSource.IsCancellationRequested)
            {
                return;
            }

            flyout.DispatcherQueue.TryEnqueue(() =>
            {
                if (!tokenSource.IsCancellationRequested)
                {
                    SetIsOpen(flyout, value);
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(GetPendingSwitchTokenSource(flyout), tokenSource))
            {
                SetPendingSwitchTokenSource(flyout, null);
            }

            tokenSource.Dispose();
        }
    }
}
