// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Caching;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Factory.Picker;
using Snap.Hutao.Remastered.Factory.Progress;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.ViewModel.Guide;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
using System.IO;
using System.Runtime.InteropServices;
using Windows.System;

namespace Snap.Hutao.Remastered.ViewModel.Setting;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class SettingStorageViewModel : Abstraction.ViewModel
{
    private readonly IFileSystemPickerInteraction fileSystemPickerInteraction;
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly IProgressFactory progressFactory;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;
    private readonly IImageCache imageCache;

    [GeneratedConstructor]
    public partial SettingStorageViewModel(IServiceProvider serviceProvider);

    [ObservableProperty]
    public partial SettingFolderViewModel? CacheFolderView { get; set; }

    [ObservableProperty]
    public partial SettingFolderViewModel? DataFolderView { get; set; }

    [ObservableProperty]
    public partial bool IsDataTransferEnabled { get; set; } = true;

    [Command("OpenBackgroundImageFolderCommand")]
    private static async Task OpenBackgroundImageFolderAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Open background image folder", "SettingStorageViewModel.Command"));
        string folder = LocalSetting.Get(SettingKeys.BackgroundImagePath, string.Empty);
        if (string.IsNullOrEmpty(folder))
        {
            folder = HutaoRuntime.GetDataBackgroundDirectory();
        }

        await Launcher.LaunchFolderPathAsync(folder);
    }

    [Command("OpenBackgroundVideoFolderCommand")]
    private static async Task OpenBackgroundVideoFolderAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Open background video folder", "SettingStorageViewModel.Command"));
        string folder = HutaoRuntime.GetDataBackgroundDirectory();
        await Launcher.LaunchFolderPathAsync(folder);
    }

    [Command("SetDataFolderCommand")]
    private async Task SetDataFolderAsync()
    {
        if (!IsDataTransferEnabled)
        {
            return;
        }

        IsDataTransferEnabled = false;
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Set data folder path", "SettingStorageViewModel.Command"));

        try
        {
            SettingStorageSetDataFolderOperation operation = new()
            {
                FileSystemPickerInteraction = fileSystemPickerInteraction,
                ContentDialogFactory = contentDialogFactory,
                ProgressFactory = progressFactory,
                Messenger = messenger,
            };

            if (await operation.TryExecuteAsync().ConfigureAwait(false))
            {
                await taskContext.SwitchToMainThreadAsync();
                try
                {
                    AppInstance.Restart(string.Empty);
                }
                catch (COMException)
                {
                    // The location is saved. The user can restart manually.
                }

                messenger.Send(InfoBarMessage.Success(SH.ViewModelSettingSetDataFolderSuccess));
            }
        }
        finally
        {
            await taskContext.SwitchToMainThreadAsync();
            IsDataTransferEnabled = true;
        }
    }

    [Command("SaveUserDataBackupCommand")]
    private async Task SaveUserDataBackupAsync()
    {
        if (!IsDataTransferEnabled)
        {
            return;
        }

        IsDataTransferEnabled = false;
        try
        {
            if (!fileSystemPickerInteraction.PickFolder(SH.ViewPageSettingUserDataBackupHeader).TryGetValue(out string? folder))
            {
                return;
            }

            string source = HutaoRuntime.DataDirectory;
            if (UrlPath.IsEqualOrSubdirectory(source, folder))
            {
                messenger.Send(InfoBarMessage.Warning(SH.ViewModelSettingStorageBackupLocation));
                return;
            }

            string archivePath = Path.Combine(folder, $"Snap.Hutao.Backup-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip");
            ContentDialog dialog = await contentDialogFactory
                .CreateForIndeterminateProgressAsync(SH.ViewModelSettingStorageBackupProgress)
                .ConfigureAwait(false);
            using (await contentDialogFactory.BlockAsync(dialog).ConfigureAwait(false))
            {
                await taskContext.SwitchToBackgroundAsync();
                UserDataTransfer.SaveBackup(source, archivePath);
            }

            messenger.Send(InfoBarMessage.Success(SH.FormatViewModelSettingStorageBackupSuccess(archivePath)));
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(ex));
        }
        finally
        {
            await taskContext.SwitchToMainThreadAsync();
            IsDataTransferEnabled = true;
        }
    }

    [Command("DeleteServerCacheFolderCommand")]
    private async Task DeleteServerCacheFolderAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Delete server cache folder", "SettingStorageViewModel.Command"));

        ContentDialogResult result = await contentDialogFactory.CreateForConfirmCancelAsync(
            SH.ViewModelSettingDeleteServerCacheFolderTitle,
            SH.ViewModelSettingDeleteServerCacheFolderContent)
            .ConfigureAwait(false);

        if (result is not ContentDialogResult.Primary)
        {
            return;
        }

        string cacheFolder = HutaoRuntime.GetDataServerCacheDirectory();
        if (Directory.Exists(cacheFolder))
        {
            Directory.Delete(cacheFolder, true);
        }

        if (DataFolderView is not null)
        {
            await DataFolderView.UpdateFolderSizeTimeoutAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }

        messenger.Send(InfoBarMessage.Success(SH.ViewModelSettingActionComplete));
    }

    [Command("RemoveDuplicateImagesCommand")]
    private async Task RemoveDuplicateImagesAsync()
    {
        ContentDialog dialog = await contentDialogFactory
            .CreateForIndeterminateProgressAsync(SH.ViewSettingImageDuplicatesProgress)
            .ConfigureAwait(false);
        using (await contentDialogFactory.BlockAsync(dialog).ConfigureAwait(false))
        {
            try
            {
                await taskContext.SwitchToBackgroundAsync();
                (int files, long bytes) = await imageCache.RemoveDuplicateFilesAsync(CancellationToken).ConfigureAwait(false);
                messenger.Send(InfoBarMessage.Success(SH.FormatViewSettingImageDuplicatesResult(files, CommunityToolkit.Common.Converters.ToFileSizeString(bytes))));
                if (CacheFolderView is not null)
                {
                    await CacheFolderView.UpdateFolderSizeTimeoutAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // The settings page was closed.
            }
            catch (Exception ex)
            {
                messenger.Send(InfoBarMessage.Error(ex));
            }
        }
    }

    [Command("ResetStaticResourceCommand")]
    private async Task ResetStaticResource()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Reset static resource", "SettingStorageViewModel.Command"));

        ContentDialog dialog = await contentDialogFactory
            .CreateForIndeterminateProgressAsync(SH.ViewModelSettingResetStaticResourceProgress)
            .ConfigureAwait(false);

        using (await contentDialogFactory.BlockAsync(dialog).ConfigureAwait(false))
        {
            await taskContext.SwitchToBackgroundAsync();
            StaticResource.FailAll();
            try
            {
                Directory.Delete(Path.Combine(HutaoRuntime.LocalCacheDirectory, nameof(ImageCache)), true);
            }
            catch (DirectoryNotFoundException ex)
            {
                // Could not find a part of the path '.*?'.
                if (HutaoNative.IsWin32(ex.HResult, WIN32_ERROR.ERROR_PATH_NOT_FOUND))
                {
                    return;
                }

                SentrySdk.CaptureException(ex);
            }
            catch (IOException ex)
            {
                if (HutaoNative.IsWin32(ex.HResult, WIN32_ERROR.ERROR_SHARING_VIOLATION))
                {
                    return;
                }

                SentrySdk.CaptureException(ex);
            }

            UnsafeLocalSetting.Set(SettingKeys.GuideState, GuideState.StaticResourceBegin);
        }

        // TODO: prompt user that restart will be non-elevated
        try
        {
            AppInstance.Restart(string.Empty);
        }
        catch (COMException ex)
        {
            if (HutaoNative.IsWin32(ex.HResult, WIN32_ERROR.ERROR_PACKAGE_UPDATING))
            {
                messenger.Send(InfoBarMessage.Error(SH.ViewModelSettingStorageRestartFailed));
            }
        }
    }
}
