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
using Snap.Hutao.Remastered.Service.Git;
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

namespace Snap.Hutao.Remastered.ViewModel;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Transient)]
public sealed partial class MainViewModel : Abstraction.ViewModel, IDisposable
{
    private const string StartupRetryKey = "MainViewModel.Startup";
    private const string UpdateRetryKey = "MainViewModel.CheckUpdate";
    private readonly ICurrentXamlWindowReference currentXamlWindowReference;
    private readonly IMetadataService metadataService;
    private readonly IUpdateService updateService;
    private readonly IUserService userService;
    private readonly INetworkRetryCoordinator networkRetryCoordinator;
    private readonly IMessenger messenger;
    private readonly App app;
    private DispatcherTimer? successHideTimer;
    private IDisposable? startupRetryRegistration;
    private IDisposable? updateRetryRegistration;
    private readonly AsyncLoadOperation startupLoad = new();
    private readonly AsyncLoadOperation updateLoad = new();

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

    public partial RepositoryCatalogService RepositoryCatalog { get; }

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
            updateRetryRegistration?.Dispose();
            Uninitialize();
        }

        base.Dispose();
    }

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        BackgroundActivityOptions.MetadataInitialization.PropertyChanged += OnMetadataInitializationPropertyChanged;
        startupRetryRegistration ??= networkRetryCoordinator.Register(StartupRetryKey, RetryStartupAsync);
        updateRetryRegistration ??= networkRetryCoordinator.Register(UpdateRetryKey, RetryUpdateAsync);

        ShowUpdateLogWindowAfterUpdate();
        NotifyIfDataFolderHasReparsePoint();
        using CancellationTokenSource startupCancellation = CancellationTokenSource.CreateLinkedTokenSource(token, XamlApplicationLifetime.ShutdownToken);
        return await RetryStartupAsync(startupCancellation.Token).ConfigureAwait(false);
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

    private async ValueTask<bool> CheckUpdateAsync(CancellationToken token)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Check for update", "MainViewModel.Command"));

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        CheckUpdateResult checkUpdateResult = await updateService.CheckUpdateAsync(timeout.Token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (XamlApplicationLifetime.Exiting || IsViewUnloaded.Value) return false;
        await updateService.TriggerUpdateAsync(checkUpdateResult, token).ConfigureAwait(false);
        return true;
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
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, CancellationToken, XamlApplicationLifetime.ShutdownToken);
        bool success = await startupLoad.RunAsync(ct => RetryStartupCoreAsync(ct).AsTask(), linked.Token).ConfigureAwait(false);
        if (success)
        {
            await PublishInitializationAsync(true, linked.Token).ConfigureAwait(false);
            // Optional updates must not hold IsInitialized false and veto X/Alt+F4.
            if (IsInitialized && !IsViewUnloaded.Value && !XamlApplicationLifetime.Exiting)
                RetryUpdateAsync(linked.Token).AsTask().SafeForget();
        }
        return success;
    }

    private async ValueTask<bool> RetryUpdateAsync(CancellationToken token)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, CancellationToken, XamlApplicationLifetime.ShutdownToken);
        token = linked.Token;
        try
        {
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            token.ThrowIfCancellationRequested();
            if (IsViewUnloaded.Value || XamlApplicationLifetime.Exiting) return true;
            if (RepositoryCatalog.IsOffline)
            {
                networkRetryCoordinator.MarkPending(UpdateRetryKey, string.Empty);
                return false;
            }

            bool success = await updateLoad.RunAsync(ct => CheckUpdateAsync(ct).AsTask(), token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (IsViewUnloaded.Value || XamlApplicationLifetime.Exiting) return true;
            if (success) networkRetryCoordinator.ClearPending(UpdateRetryKey);
            else networkRetryCoordinator.MarkPending(UpdateRetryKey, SH.ViewModelMainNetworkConnectionFailedWillAutoRetry);
            return success;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsNetworkRelatedException(exception))
        {
            token.ThrowIfCancellationRequested();
            if (!IsViewUnloaded.Value && !XamlApplicationLifetime.Exiting)
                networkRetryCoordinator.MarkPending(UpdateRetryKey, SH.ViewModelMainNetworkConnectionFailedWillAutoRetry);
            return false;
        }
    }

    private async ValueTask<bool> RetryStartupCoreAsync(CancellationToken token)
    {
        try
        {
            if (!await metadataService.InitializepublicAsync(token).ConfigureAwait(false))
            {
                networkRetryCoordinator.MarkPending(StartupRetryKey, SH.ViewModelMainNetworkConnectionFailedWillAutoRetry);
                return false;
            }

            await userService.RetryResumeUninitializedUsersAsync(token).ConfigureAwait(false);
            networkRetryCoordinator.ClearPending(StartupRetryKey);
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (IsNetworkRelatedException(ex))
            {
                networkRetryCoordinator.MarkPending(StartupRetryKey, SH.ViewModelMainNetworkConnectionFailedWillAutoRetry);
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
            OperationCanceledException => true,
            _ => false,
        };
    }
}
