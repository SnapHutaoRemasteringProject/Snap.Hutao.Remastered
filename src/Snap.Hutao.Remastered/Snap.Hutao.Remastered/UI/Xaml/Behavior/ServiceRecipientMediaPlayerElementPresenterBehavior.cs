// Copyright (c) Snap HuTao RP. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Animations;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Service.BackgroundMediaPlayer;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Media.Animation;
using Snap.Hutao.Remastered.UI.Xaml.View.Window;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

public sealed partial class ServiceRecipientMediaPlayerElementPresenterBehavior : BehaviorBase<MediaPlayerElement>, IDisposable, IRecipient<Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage>
{
    private readonly CancellationTokenSource unloadCts = new();

    private IBackgroundMediaPlayerService? backgroundMediaPlayerService;
    private AppWindow? appWindow;
    private OverlappedPresenterState previousState = OverlappedPresenterState.Restored;

    public void Dispose()
    {
        unloadCts.Dispose();
    }

    protected override void OnAssociatedObjectLoaded()
    {
        if (AssociatedObject.XamlRoot.XamlContext()?.ServiceProvider is { } serviceProvider)
        {
            backgroundMediaPlayerService = serviceProvider.GetRequiredService<IBackgroundMediaPlayerService>();
            IMessenger messenger = serviceProvider.GetRequiredService<IMessenger>();
            messenger.Register<Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage>(this);
            PrivateUpdateMediaPlayerElementAsync(unloadCts.Token).SafeForget();
        }

        if (MainWindow.Instance.AppWindow is { } window)
        {
            appWindow = window;
            if (window.Presenter is OverlappedPresenter presenter)
            {
                previousState = presenter.State;
            }

            window.Changed += OnAppWindowChanged;
        }
    }

    protected override bool Uninitialize()
    {
        if (appWindow is not null)
        {
            appWindow.Changed -= OnAppWindowChanged;
            appWindow = null;
        }

        unloadCts.Cancel();

        if (AssociatedObject is { } mediaElement)
        {
            mediaElement.Source = null;
            mediaElement.SetMediaPlayer(null);
        }

        if (AssociatedObject.XamlRoot.XamlContext()?.ServiceProvider is { } serviceProvider)
        {
            IMessenger messenger = serviceProvider.GetRequiredService<IMessenger>();
            messenger.UnregisterAll(this);
        }

        backgroundMediaPlayerService?.Stop();

        return base.Uninitialize();
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (XamlApplicationLifetime.Exiting)
        {
            return;
        }

        if (sender.Presenter is OverlappedPresenter presenter && presenter.State != previousState)
        {
            previousState = presenter.State;
            switch (presenter.State)
            {
                case OverlappedPresenterState.Minimized:
                    backgroundMediaPlayerService?.Stop();
                    if (AssociatedObject is { } mediaElement)
                    {
                        mediaElement.Source = null;
                        mediaElement.SetMediaPlayer(null);
                    }

                    break;
                case OverlappedPresenterState.Restored:
                case OverlappedPresenterState.Maximized:
                    PrivateUpdateMediaPlayerElementAsync(unloadCts.Token).SafeForget();
                    break;
            }
        }
    }

    [Command("UpdateMediaPlayerElementCommand")]
    private void UpdateMediaPlayerElement()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Update media element", "ServiceRecipientMediaPlayerElementPresenterBehavior.Command"));
        PrivateUpdateMediaPlayerElementAsync(unloadCts.Token).SafeForget();
    }

    private async ValueTask PrivateUpdateMediaPlayerElementAsync(CancellationToken token = default)
    {
        if (AssociatedObject is not { } mediaElement || backgroundMediaPlayerService is null)
        {
            return;
        }

        ITaskContext taskContext = TaskContext.GetForDependencyObject(mediaElement);

        token.ThrowIfCancellationRequested();

        try
        {
            if (XamlApplicationLifetime.Exiting)
            {
                return;
            }

            await AnimationBuilder
                .Create()
                .Opacity(
                    to: 0D,
                    duration: Constants.ImageOpacityFadeInOut,
                    easingType: EasingType.Quartic,
                    easingMode: EasingMode.EaseInOut)
                .StartAsync(mediaElement, token)
                .ConfigureAwait(false);

            await backgroundMediaPlayerService.UpdateMediaPlayerElementAsync(mediaElement, token).ConfigureAwait(false);

            await taskContext.SwitchToMainThreadAsync();

            double targetOpacity = mediaElement.Source is null ? 0 : 1;

            await AnimationBuilder
                .Create()
                .Opacity(
                    to: targetOpacity,
                    duration: Constants.ImageOpacityFadeInOut,
                    easingType: EasingType.Quartic,
                    easingMode: EasingMode.EaseInOut)
                .StartAsync(mediaElement, token)
                .ConfigureAwait(false);
        }
        catch (COMException)
        {
            // ignore
        }
    }

    public void Receive(Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage message)
    {
        PrivateUpdateMediaPlayerElementAsync(unloadCts.Token).SafeForget();
    }
}
