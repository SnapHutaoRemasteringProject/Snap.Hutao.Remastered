// Copyright (c) Snap HuTao RP. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Service.BackgroundMediaPlayer;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Media.Animation;
using Snap.Hutao.Remastered.UI.Xaml.View.Window;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

public sealed partial class ServiceRecipientMediaPlayerElementPresenterBehavior : BehaviorBase<MediaPlayerElement>, IDisposable, IRecipient<Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage>
{
    private CancellationTokenSource? unloadCts;
    private IMessenger? messenger;
    private readonly AsyncLock updateLock = new();

    private IBackgroundMediaPlayerService? backgroundMediaPlayerService;
    private ILogger<ServiceRecipientMediaPlayerElementPresenterBehavior>? logger;
    private AppWindow? appWindow;
    private OverlappedPresenterState previousState = OverlappedPresenterState.Restored;

    public void Dispose()
    {
        if (appWindow is not null)
        {
            appWindow.Changed -= OnAppWindowChanged;
            appWindow = null;
        }

        backgroundMediaPlayerService?.SetWindowVisible(false);
        unloadCts?.Cancel();
        unloadCts?.Dispose();
        unloadCts = null;
        messenger?.UnregisterAll(this);
        messenger = null;
    }

    protected override void OnAssociatedObjectLoaded()
    {
        if (XamlApplicationLifetime.Exiting) return;
        unloadCts?.Cancel();
        unloadCts?.Dispose();
        unloadCts = new();
        if (AssociatedObject.XamlRoot.XamlContext()?.ServiceProvider is { } serviceProvider)
        {
            backgroundMediaPlayerService = serviceProvider.GetRequiredService<IBackgroundMediaPlayerService>();
            logger = serviceProvider.GetRequiredService<ILogger<ServiceRecipientMediaPlayerElementPresenterBehavior>>();
            messenger?.UnregisterAll(this);
            messenger = serviceProvider.GetRequiredService<IMessenger>();
            messenger.Register<Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage>(this);
        }

        if (MainWindow.Instance.AppWindow is { } window)
        {
            if (appWindow is not null) appWindow.Changed -= OnAppWindowChanged;
            appWindow = window;
            if (window.Presenter is OverlappedPresenter presenter)
            {
                previousState = presenter.State;
            }

            UpdatePlaybackVisibility(window);
            window.Changed += OnAppWindowChanged;
        }

        PrivateUpdateMediaPlayerElementAsync(unloadCts.Token).SafeForget();
    }

    protected override bool Uninitialize()
    {
        backgroundMediaPlayerService?.SetWindowVisible(false);
        if (appWindow is not null)
        {
            appWindow.Changed -= OnAppWindowChanged;
            appWindow = null;
        }

        unloadCts?.Cancel();
        unloadCts?.Dispose();
        unloadCts = null;
        if (AssociatedObject is { } mediaElement)
        {
            mediaElement.SetMediaPlayer(null);
        }

        messenger?.UnregisterAll(this);
        messenger = null;

        backgroundMediaPlayerService?.Stop();

        return base.Uninitialize();
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (XamlApplicationLifetime.Exiting)
        {
            return;
        }

        UpdatePlaybackVisibility(sender);
        if (sender.Presenter is OverlappedPresenter presenter && presenter.State != previousState)
        {
            previousState = presenter.State;
            switch (presenter.State)
            {
                case OverlappedPresenterState.Minimized:
                    // Detach the element from its player first: Stop() disposes the player, and the
                    // element must not touch it afterwards.
                    if (AssociatedObject is { } mediaElement)
                    {
                        mediaElement.SetMediaPlayer(null);
                    }

                    backgroundMediaPlayerService?.Stop();
                    break;
                case OverlappedPresenterState.Restored:
                case OverlappedPresenterState.Maximized:
                    UpdateMediaPlayerElement();
                    break;
            }
        }
        else if (args.DidVisibilityChange && sender.IsVisible)
        {
            UpdateMediaPlayerElement();
        }
    }

    private void UpdatePlaybackVisibility(AppWindow window)
    {
        bool isVisible = window.IsVisible && window.Presenter is not OverlappedPresenter { State: OverlappedPresenterState.Minimized };
        backgroundMediaPlayerService?.SetWindowVisible(isVisible);
    }

    [Command("UpdateMediaPlayerElementCommand")]
    private void UpdateMediaPlayerElement()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Update media element", "ServiceRecipientMediaPlayerElementPresenterBehavior.Command"));
        if (!XamlApplicationLifetime.Exiting && unloadCts is { IsCancellationRequested: false } run)
            PrivateUpdateMediaPlayerElementAsync(run.Token).SafeForget();
    }

    private async ValueTask PrivateUpdateMediaPlayerElementAsync(CancellationToken token = default)
    {
        if (XamlApplicationLifetime.Exiting || AssociatedObject is not { IsLoaded: true } mediaElement || backgroundMediaPlayerService is null)
        {
            return;
        }

        ITaskContext taskContext = TaskContext.GetForDependencyObject(mediaElement);

        using (await updateLock.LockAsync().ConfigureAwait(false))
        {
            token.ThrowIfCancellationRequested();
            if (XamlApplicationLifetime.Exiting) return;
            await taskContext.SwitchToMainThreadAsync();
            token.ThrowIfCancellationRequested();
            try
            {
                if (XamlApplicationLifetime.Exiting)
                {
                    return;
                }

                await OpacityTransition.StartAsync(mediaElement, 0D, Constants.ImageOpacityFadeInOut, token)
                    .ConfigureAwait(false);

                await backgroundMediaPlayerService.UpdateMediaPlayerElementAsync(mediaElement, token).ConfigureAwait(false);

                await taskContext.SwitchToMainThreadAsync();
                token.ThrowIfCancellationRequested();
                if (XamlApplicationLifetime.Exiting) return;

                double targetOpacity = mediaElement.MediaPlayer?.Source is null ? 0 : 1;

                await OpacityTransition.StartAsync(mediaElement, targetOpacity, Constants.ImageOpacityFadeInOut, token)
                    .ConfigureAwait(false);
            }
            catch (COMException ex)
            {
                // 0x8001010E The element was closed while applying its source.
                logger?.LogWarning(ex, "Failed to update the background media player element");
            }
        }
    }

    public void Receive(Snap.Hutao.Remastered.Service.BackgroundMediaPlayer.Message.BackgroundMediaOptionsChangedMessage message)
    {
        UpdateMediaPlayerElement();
    }
}
