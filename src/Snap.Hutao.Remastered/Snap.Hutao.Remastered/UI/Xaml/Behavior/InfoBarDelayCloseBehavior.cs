// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

[SuppressMessage("", "CA1001")]
[DependencyProperty<int>("MilliSecondsDelay", NotNull = true)]
public sealed partial class InfoBarDelayCloseBehavior : BehaviorBase<InfoBar>
{
    private CancellationTokenSource? userCloseTokenSource;

    protected override void OnAssociatedObjectLoaded()
    {
        if (XamlApplicationLifetime.Exiting) return;
        userCloseTokenSource?.Cancel();
        userCloseTokenSource?.Dispose();
        userCloseTokenSource = new();
        AssociatedObject.Closed -= OnInfoBarClosed;
        AssociatedObject.Closed += OnInfoBarClosed;
        if (MilliSecondsDelay > 0)
        {
            PrivateDelayAsync(AssociatedObject, userCloseTokenSource.Token).SafeForget();
        }
    }

    protected override bool Uninitialize()
    {
        userCloseTokenSource?.Cancel();
        userCloseTokenSource?.Dispose();
        userCloseTokenSource = null;
        AssociatedObject.Closed -= OnInfoBarClosed;
        return base.Uninitialize();
    }

    private async ValueTask PrivateDelayAsync(InfoBar infoBar, CancellationToken token)
    {
        ITaskContext taskContext = TaskContext.GetForDependencyObject(infoBar);
        try
        {
            await Task.Delay(MilliSecondsDelay, token).ConfigureAwait(false);
            if (token.IsCancellationRequested || XamlApplicationLifetime.Exiting) return;
            await taskContext.SwitchToMainThreadAsync();
            if (!token.IsCancellationRequested && !XamlApplicationLifetime.Exiting && infoBar.IsLoaded) infoBar.IsOpen = false;
        }
        catch (OperationCanceledException)
        {
            return;
        }

    }

    private void OnInfoBarClosed(InfoBar infoBar, InfoBarClosedEventArgs args)
    {
        if (args.Reason is InfoBarCloseReason.CloseButton)
        {
            userCloseTokenSource?.Cancel();
        }

        AssociatedObject.Closed -= OnInfoBarClosed;
    }
}
