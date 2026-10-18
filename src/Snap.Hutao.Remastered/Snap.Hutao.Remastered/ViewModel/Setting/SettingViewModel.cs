// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Core.Shell;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.Update;
using Snap.Hutao.Remastered.Web;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
using System.Diagnostics;
using Windows.Foundation;

namespace Snap.Hutao.Remastered.ViewModel.Setting;

[Service(ServiceLifetime.Scoped)]
public sealed partial class SettingViewModel : Abstraction.ViewModel, INavigationRecipient
{
    public const string UIGFImportExport = nameof(UIGFImportExport);

    private readonly IShellLinkInterop shellLinkInterop;
    private readonly IUpdateService updateService;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;
    private readonly IToastNotificationService toastNotificationService;

    private readonly WeakReference<ScrollViewer> weakScrollViewer = new(default!);
    private readonly WeakReference<Border> weakGachaLogBorder = new(default!);

    [GeneratedConstructor]
    public partial SettingViewModel(IServiceProvider serviceProvider);

    public partial SettingGeetestViewModel Geetest { get; }

    public partial SettingAppearanceViewModel Appearance { get; }

    public partial SettingStorageViewModel Storage { get; }

    public partial SettingHotKeyViewModel HotKey { get; }

    public partial SettingHomeViewModel Home { get; }

    public partial SettingGameViewModel Game { get; }

    public partial SettingGachaLogViewModel GachaLog { get; }

    public partial SettingWebViewViewModel WebView { get; }

    public partial AppOptions AppOptions { get; }

    partial void PostConstruct(IServiceProvider serviceProvider)
    {
        AppOptions.AutoRestartAsAdmin.PropertyChanged += OnStartupOptionChanged;
        AppOptions.IsStartupEnabled.PropertyChanged += OnStartupOptionChanged;
        AppOptions.IsStartupAsAdminEnabled.PropertyChanged += OnStartupOptionChanged;
    }

    private void OnStartupOptionChanged(object? sender, PropertyChangedEventArgs args)
    {
        OnPropertyChanged(nameof(AutoRestartAsAdminEnabled));
        OnPropertyChanged(nameof(IsStartupEnabled));
        OnPropertyChanged(nameof(IsStartupAsAdminEnabled));
        OnPropertyChanged(nameof(IsStartupAsAdminCardEnabled));
    }

    public override void Dispose()
    {
        AppOptions.AutoRestartAsAdmin.PropertyChanged -= OnStartupOptionChanged;
        AppOptions.IsStartupEnabled.PropertyChanged -= OnStartupOptionChanged;
        AppOptions.IsStartupAsAdminEnabled.PropertyChanged -= OnStartupOptionChanged;
        base.Dispose();
    }

    [ObservableProperty]
    public partial string? UpdateInfo { get; set; }

    // TODO: Replace with IObservableProperty
    public NameValue<ServerDomainMode>? SelectedServerDomainMode
    {
        get => field ??= AppOptions.ServerDomainModes.Single(t => t.Value == AppOptions.ServerDomainMode.Value);
        set
        {
            if (SetProperty(ref field, value) && value is not null)
            {
                AppOptions.ServerDomainMode.Value = value.Value;
            }
        }
    }

    public bool AutoRestartAsAdminEnabled
    {
        get => AppOptions?.AutoRestartAsAdmin?.Value ?? false;
        set
        {
            if (AppOptions is null)
            {
                return;
            }

            if (AppOptions.AutoRestartAsAdmin.Value == value)
            {
                return;
            }

            if (value && !IsStartupAsAdminEnabled)
            {
                IsStartupAsAdminEnabled = true;
            }

            AppOptions.AutoRestartAsAdmin.Value = value;
            OnPropertyChanged(nameof(AutoRestartAsAdminEnabled));
            OnPropertyChanged(nameof(IsStartupAsAdminCardEnabled));
            OnPropertyChanged(nameof(IsStartupAsAdminEnabled));
        }
    }

    public bool IsStartupAsAdminCardEnabled
    {
        get => IsStartupEnabled && !AutoRestartAsAdminEnabled;
    }

    public bool IsStartupEnabled
    {
        get => AppOptions?.IsStartupEnabled?.Value ?? false;
        set
        {
            if (AppOptions is null)
            {
                return;
            }

            if (AppOptions.IsStartupEnabled.Value == value)
            {
                return;
            }

            AppOptions.IsStartupEnabled.Value = value;
            OnStartupEnabledChanged(value);
            OnPropertyChanged(nameof(IsStartupEnabled));
            OnPropertyChanged(nameof(IsStartupAsAdminCardEnabled));
            OnPropertyChanged(nameof(IsStartupAsAdminEnabled));
        }
    }

    public bool IsStartupAsAdminEnabled
    {
        get => AppOptions?.IsStartupAsAdminEnabled?.Value ?? false;
        set
        {
            if (AppOptions is null)
            {
                return;
            }

            if (AppOptions.IsStartupAsAdminEnabled.Value == value)
            {
                return;
            }

            AppOptions.IsStartupAsAdminEnabled.Value = value;
            OnStartupAsAdminEnabledChanged(value);
            OnPropertyChanged(nameof(IsStartupAsAdminEnabled));
        }
    }

    public void AttachXamlElement(ScrollViewer scrollViewer, Border gachaLogBorder)
    {
        weakScrollViewer.SetTarget(scrollViewer);
        weakGachaLogBorder.SetTarget(gachaLogBorder);
    }

    public async ValueTask<bool> ReceiveAsync(INavigationExtraData data, CancellationToken token)
    {
        if (!await Initialization.Task.ConfigureAwait(false))
        {
            return false;
        }

        if (!weakScrollViewer.TryGetTarget(out ScrollViewer? scrollViewer) ||
            !weakGachaLogBorder.TryGetTarget(out Border? gachaLogBorder))
        {
            return false;
        }

        if (data.Data is UIGFImportExport)
        {
            await taskContext.SwitchToMainThreadAsync();
            Point point = gachaLogBorder.TransformToVisual(scrollViewer).TransformPoint(new(0, 0));
            scrollViewer.ChangeView(null, point.Y, null, true);
            return true;
        }

        return false;
    }

    protected override ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        MakeSubViewModel([Geetest, Appearance, Storage, HotKey, Home, Game, GachaLog, WebView]);

        Storage.CacheFolderView = new(taskContext, HutaoRuntime.LocalCacheDirectory);
        Storage.DataFolderView = new(taskContext, HutaoRuntime.DataDirectory);

        UpdateInfo = updateService.UpdateInfo;

        if (AutoRestartAsAdminEnabled && !IsStartupAsAdminEnabled)
        {
            IsStartupAsAdminEnabled = true;
        }

        // Sync existing SQLite value to LocalSetting for Bootstrap to read on next startup.
        // Bootstrap runs before DI/SQLite is initialized, so it can only read from LocalSetting.
        LocalSetting.Set(SettingKeys.AutoRestartAsAdmin, AutoRestartAsAdminEnabled);

        return ValueTask.FromResult(true);
    }

    [Command("CheckUpdateCommand")]
    private async Task CheckUpdateAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Check update", "SettingViewModel.Command"));

        await taskContext.SwitchToBackgroundAsync();

        CheckUpdateResult result = await updateService.CheckUpdateAsync().ConfigureAwait(false);
        await taskContext.InvokeOnMainThreadAsync(() => UpdateInfo = result.Kind switch
        {
            CheckUpdateResultKind.UpdateAvailable => SH.FormatViewModelSettingUpdateAvailable(result.PackageInformation?.Version.ToString()),
            CheckUpdateResultKind.AlreadyUpdated => SH.ViewModelSettingAlreadyUpdated,
            CheckUpdateResultKind.VersionApiInvalidResponse or CheckUpdateResultKind.VersionApiInvalidSha256 => SH.ViewModelSettingCheckUpdateFailed,
            _ => default!,
        }).ConfigureAwait(false);

        await updateService.TriggerUpdateAsync(result).ConfigureAwait(false);
    }

    [Command("RestartAsElevatedCommand")]
    private void RestartAsElevated()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Restart as elevated", "NotifyIconViewModel.Command"));

        try
        {
            string? path = RuntimeEnvironment.IsUnpackaged
                ? Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName
                : $"shell:AppsFolder\\{HutaoRuntime.FamilyName}!App";

            ArgumentNullException.ThrowIfNull(path);
            ProcessFactory.StartUsingShellExecuteRunAs(path);
        }
        catch (Win32Exception ex)
        {
            // 组或资源的状态不是执行请求操作的正确状态
            if (ex.HResult is HRESULT.E_FAIL)
            {
                try
                {
                    toastNotificationService.ShowText(SH.ViewModelNotifyIconRestartAsElevatedErrorHint);
                    return;
                }
                catch
                {
                    // Ignored
                }
            }

            throw;
        }

        // Current process will exit in PrivatePipeServer
    }

    [Command("CreateDesktopShortcutCommand")]
    private void CreateDesktopShortcutForElevatedLaunchAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Create desktop shortcut for elevated launch", "SettingViewModel.Command"));

        _ = shellLinkInterop.TryCreateDesktopShortcut()
            ? messenger.Send(InfoBarMessage.Success(SH.ViewModelSettingActionComplete))
            : messenger.Send(InfoBarMessage.Warning(SH.ViewModelSettingCreateDesktopShortcutFailed));
    }

    [Command("CreateGameLaunchShortcutCommand")]
    private void CreateGameLaunchShortcut()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Create game launch shortcut", "SettingViewModel.Command"));

        _ = shellLinkInterop.TryCreateGameLaunchShortcut()
            ? messenger.Send(InfoBarMessage.Success(SH.ViewModelSettingActionComplete))
            : messenger.Send(InfoBarMessage.Warning(SH.ViewModelSettingCreateDesktopShortcutFailed));
    }

    private void OnStartupEnabledChanged(bool isEnabled)
    {
        // Only manage task scheduler when process is elevated (the UI has checked) 
        try
        {
            if (isEnabled)
            {
                BOOL runElevated = IsStartupAsAdminEnabled;
                HutaoNative.Instance.CreateAutoStartTaskForThisUser(runElevated);
            }
            else
            {
                // Delete the task if disabled
                HutaoNative.Instance.DeleteAutoStartTaskForThisUser();
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent UI disruption
            SentrySdk.CaptureException(ex);
        }
    }

    private void OnStartupAsAdminEnabledChanged(bool isAdminEnabled)
    {
        // Only manage task scheduler when process is elevated and startup is enabled
        if (!Environment.IsPrivilegedProcess || !IsStartupEnabled)
        {
            return;
        }

        try
        {
            // Recreate the task with the new elevation level
            HutaoNative.Instance.DeleteAutoStartTaskForThisUser();
            HutaoNative.Instance.CreateAutoStartTaskForThisUser(isAdminEnabled ? (BOOL)true : (BOOL)false);
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent UI disruption
            SentrySdk.CaptureException(ex);
        }
    }
}
