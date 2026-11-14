// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.BackgroundImage;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using Snap.Hutao.Remastered.UI.Xaml.Media.Animation;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

public sealed partial class ServiceRecipientBackgroundImagePresenterBehavior : BehaviorBase<Image>, IDisposable
{
    private CancellationTokenSource? unloadCts;
    private readonly AsyncLock backgroundImageLock = new();

    private IBackgroundImageService? backgroundImageService;
    private ILogger<ServiceRecipientBackgroundImagePresenterBehavior>? logger;
    private BackgroundImage? previousBackgroundImage;

    private IObservableProperty<BackgroundImageType>? BackgroundImageTypeCallback { get; set; }
    private IObservableProperty<string>? BackgroundImagePathCallback { get; set; }

    public void Dispose()
    {
        BackgroundImageTypeCallback = null;
        BackgroundImagePathCallback = null;
        unloadCts?.Cancel();
        unloadCts?.Dispose();
        unloadCts = null;
    }

    protected override void OnAssociatedObjectLoaded()
    {
        if (XamlApplicationLifetime.Exiting) return;
        unloadCts?.Cancel();
        unloadCts?.Dispose();
        unloadCts = new();
        if (AssociatedObject.Source is null)
        {
            previousBackgroundImage = null;
        }
        if (AssociatedObject.XamlRoot.XamlContext()?.ServiceProvider is { } serviceProvider)
        {
            backgroundImageService = serviceProvider.GetRequiredService<IBackgroundImageService>();
            logger = serviceProvider.GetRequiredService<ILogger<ServiceRecipientBackgroundImagePresenterBehavior>>();
            AppOptions appOptions = serviceProvider.GetRequiredService<AppOptions>();
            BackgroundImageTypeCallback = appOptions.BackgroundImageType
                .WithValueChangedCallback(static (type, behavior) => behavior.UpdateBackground(true), this);
            BackgroundImagePathCallback = appOptions.BackgroundImagePath
                .WithValueChangedCallback(static (path, behavior) =>
                {
                    if (behavior.BackgroundImageTypeCallback?.Value is BackgroundImageType.LocalFolder)
                    {
                        behavior.UpdateBackground(true);
                    }
                }, this);
            PrivateUpdateBackgroundAsync(false, unloadCts.Token).SafeForget();
        }
    }

    protected override bool Uninitialize()
    {
        BackgroundImageTypeCallback = null;
        BackgroundImagePathCallback = null;
        unloadCts?.Cancel();
        unloadCts?.Dispose();
        unloadCts = null;
        return base.Uninitialize();
    }

    [Command("UpdateBackgroundCommand")]
    private void UpdateBackground()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Update background image", "ServiceRecipientBackgroundImagePresenterBehavior.Command"));
        UpdateBackground(false);
    }

    private void UpdateBackground(bool forceRefresh)
    {
        if (!XamlApplicationLifetime.Exiting && unloadCts is { IsCancellationRequested: false } run)
            PrivateUpdateBackgroundAsync(forceRefresh, run.Token).SafeForget();
    }

    private async ValueTask PrivateUpdateBackgroundAsync(bool forceRefresh, CancellationToken token = default)
    {
        if (XamlApplicationLifetime.Exiting || AssociatedObject is not { IsLoaded: true } backgroundImagePresenter || backgroundImageService is null)
        {
            return;
        }

        ITaskContext taskContext = TaskContext.GetForDependencyObject(backgroundImagePresenter);

        using (await backgroundImageLock.LockAsync().ConfigureAwait(false))
        {
            token.ThrowIfCancellationRequested();
            (bool shouldRefresh, BackgroundImage? backgroundImage) = await backgroundImageService.GetNextBackgroundImageAsync(forceRefresh ? default : previousBackgroundImage, token).ConfigureAwait(false);

            if (shouldRefresh || forceRefresh)
            {
                token.ThrowIfCancellationRequested();
                await taskContext.SwitchToMainThreadAsync();
                token.ThrowIfCancellationRequested();

                try
                {
                    await OpacityTransition.StartAsync(backgroundImagePresenter, 0D, Constants.ImageOpacityFadeInOut, token)
                        .ConfigureAwait(false);

                    if (XamlApplicationLifetime.Exiting)
                    {
                        return;
                    }

                    await taskContext.SwitchToMainThreadAsync();
                    token.ThrowIfCancellationRequested();
                    backgroundImagePresenter.Source = backgroundImage is null ? null : new BitmapImage(backgroundImage.Path.ToUri());

                    double targetOpacity = backgroundImage is null
                        ? 0
                        : ThemeHelper.IsDarkMode(backgroundImagePresenter.ActualTheme)
                            ? 1 - backgroundImage.Luminance
                            : backgroundImage.Luminance;

                    await OpacityTransition.StartAsync(backgroundImagePresenter, targetOpacity, Constants.ImageOpacityFadeInOut, token)
                        .ConfigureAwait(false);

                    // A canceled/failed transition must remain retryable, including when
                    // the local folder has only one image and the view is loaded again.
                    previousBackgroundImage = backgroundImage;
                }
                catch (COMException exception)
                {
                    if (!XamlApplicationLifetime.Exiting && !token.IsCancellationRequested)
                    {
                        logger?.LogWarning(exception, "Failed to present background image (HResult=0x{HResult:X8})", exception.HResult);
                    }
                }
            }
        }
    }
}
