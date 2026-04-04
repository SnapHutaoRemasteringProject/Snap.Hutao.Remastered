// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications.Builder;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Shell;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.Update;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
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

    [ObservableProperty]
    public partial string? UpdateInfo { get; set; }

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
    private static void RestartAsElevated()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Restart as elevated", "NotifyIconViewModel.Command"));

        try
        {
            ProcessFactory.StartUsingShellExecuteRunAs($"shell:AppsFolder\\{HutaoRuntime.FamilyName}!App");
        }
        catch (Win32Exception ex)
        {
            // 组或资源的状态不是执行请求操作的正确状态
            if (ex.HResult is HRESULT.E_FAIL)
            {
                try
                {
                    new AppNotificationBuilder().AddText(SH.ViewModelNotifyIconRestartAsElevatedErrorHint).Show();
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

    private void OnStartupEnabledChanged(bool isEnabled)
    {
        // Only manage task scheduler when process is elevated
        if (!Environment.IsPrivilegedProcess)
        {
            return;
        }

        try
        {
            if (isEnabled)
            {
                // Determine if task should be created with elevated privileges
                BOOL runElevated = AppOptions?.AutoRestartAsAdmin.Value ?? false;
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
