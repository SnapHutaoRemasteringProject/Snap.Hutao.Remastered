// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.BackgroundActivity;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.Network;
using Snap.Hutao.Remastered.Service.Update;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.UI.Xaml;
using Snap.Hutao.Remastered.UI.Xaml.Behavior.Action;
using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using Snap.Hutao.Remastered.UI.Xaml.View.Window.WebView2;
using Snap.Hutao.Remastered.Web;
using Snap.Hutao.Remastered.Web.Request.Builder;
using System.Net.Http;
using System.IO;
using Windows.Networking.Connectivity;

namespace Snap.Hutao.Remastered.ViewModel;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Transient)]
public sealed partial class MainViewModel : Abstraction.ViewModel, IDisposable
{
    private readonly ICurrentXamlWindowReference currentXamlWindowReference;
    private readonly IMetadataService metadataService;
    private readonly IUpdateService updateService;
    private readonly IUserService userService;
    private readonly INetworkRetryCoordinator networkRetryCoordinator;
    private readonly IMessenger messenger;
    private readonly App app;
    private DispatcherTimer? successHideTimer;
    private IDisposable? startupRetryRegistration;

    [GeneratedConstructor]
    public partial MainViewModel(IServiceProvider serviceProvider);

    public static string? Title { get => HutaoRuntime.GetDisplayName(); }

    public static bool IsDebug
    {
        get =>
#if DEBUG
            true;
#else
            false;
#endif
    }

    public partial AppOptions AppOptions { get; }

    public partial BackgroundActivityOptions BackgroundActivityOptions { get; }

    [ObservableProperty]
    public partial bool IsFlyoutOpen { get; set; }

    public override void Dispose()
    {
        using (CriticalSection.Enter())
        {
            successHideTimer?.Stop();
            BackgroundActivityOptions.MetadataInitialization.PropertyChanged -= OnMetadataInitializationPropertyChanged;
            startupRetryRegistration?.Dispose();
            Uninitialize();
        }

        base.Dispose();
    }

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        BackgroundActivityOptions.MetadataInitialization.PropertyChanged += OnMetadataInitializationPropertyChanged;
        startupRetryRegistration ??= networkRetryCoordinator.Register("MainViewModel.Startup", RetryStartupAsync);

        ShowUpdateLogWindowAfterUpdate();
        NotifyIfDataFolderHasReparsePoint();
        await RetryStartupAsync(token).ConfigureAwait(false);

        return true;
    }

    [Command("InvertAppThemeCommand")]
    private void InvertAppTheme()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Invert app theme", "MainViewModel.Command"));
        FrameworkTheming.SetTheme(ThemeHelper.ApplicationToFrameworkInvert(app.RequestedTheme));
    }

    private void ShowUpdateLogWindowAfterUpdate()
    {
        if (LocalSetting.Get(SettingKeys.AlwaysIsFirstRunAfterUpdate, false) || XamlApplicationLifetime.IsFirstRunAfterUpdate)
        {
            // Check if the window showed, only set to false if it is shown
            if (ShowWebView2WindowAction.TryShow<UpdateLogContentProvider>(currentXamlWindowReference.XamlRoot) is not null)
            {
                SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Show update log window", "MainViewModel.Command"));
                XamlApplicationLifetime.IsFirstRunAfterUpdate = false;
            }
        }
    }

    private async ValueTask CheckUpdateAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Check for update", "MainViewModel.Command"));

        CheckUpdateResult checkUpdateResult = await updateService.CheckUpdateAsync().ConfigureAwait(false);
        await updateService.TriggerUpdateAsync(checkUpdateResult).ConfigureAwait(false);
    }

    private void NotifyIfDataFolderHasReparsePoint()
    {
        if (new DirectoryInfo(HutaoRuntime.DataDirectory).Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateDebug("Data folder has reparse point", "MainViewModel.Command"));
            messenger.Send(InfoBarMessage.Warning(SH.FormatViewModelTitleDataFolderHasReparsepoint(HutaoRuntime.DataDirectory)));
        }
    }

    private void OnMetadataInitializationPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BackgroundActivity.IsCompletedSuccessfully))
        {
            BackgroundActivity? backgroundActivity = sender as BackgroundActivity;
            if (backgroundActivity?.IsCompletedSuccessfully == true)
            {
                StartSuccessHideTimer();
            }
        }
    }

    private void StartSuccessHideTimer()
    {
        successHideTimer?.Stop();
        successHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        successHideTimer.Tick += (s, e) =>
        {
            BackgroundActivityOptions.MetadataInitialization.ResetCompletionStatus();
            successHideTimer?.Stop();
        };
        successHideTimer.Start();
    }

    private async ValueTask<bool> RetryStartupAsync(CancellationToken token)
    {
        if (!HasInternetAccess())
        {
            networkRetryCoordinator.MarkPending("MainViewModel.Startup", SH.ViewModelMainNetworkUnavailableWillAutoRetry);
            return false;
        }

        try
        {
            if (!await metadataService.InitializepublicAsync(token).ConfigureAwait(false))
            {
                networkRetryCoordinator.MarkPending("MainViewModel.Startup", SH.ViewModelMainNetworkConnectionFailedWillAutoRetry);
                return false;
            }

            await userService.RetryResumeUninitializedUsersAsync(token).ConfigureAwait(false);
            await CheckUpdateAsync().ConfigureAwait(false);
            networkRetryCoordinator.ClearPending("MainViewModel.Startup");
            return true;
        }
        catch (Exception ex)
        {
            if (IsNetworkRelatedException(ex))
            {
                networkRetryCoordinator.MarkPending("MainViewModel.Startup", SH.ViewModelMainNetworkConnectionFailedWillAutoRetry);
                return false;
            }

            SentrySdk.CaptureException(ex);
            throw;
        }
    }

    private static bool IsNetworkRelatedException(Exception ex)
    {
        return ex switch
        {
            HttpRequestException httpRequestException => HttpRequestExceptionHandling.HttpRequestExceptionToNetworkError(httpRequestException) is not NetworkError.NULL,
            TimeoutException => true,
            TaskCanceledException => true,
            _ => false,
        };
    }

    private static bool HasInternetAccess()
    {
        return NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel() is NetworkConnectivityLevel.InternetAccess;
    }
}