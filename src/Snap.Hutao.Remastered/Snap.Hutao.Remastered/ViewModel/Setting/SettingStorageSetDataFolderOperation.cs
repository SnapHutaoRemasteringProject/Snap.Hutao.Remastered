// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Factory.Picker;
using Snap.Hutao.Remastered.Factory.Progress;
using Snap.Hutao.Remastered.Service.Notification;
using System.IO;

namespace Snap.Hutao.Remastered.ViewModel.Setting;

public sealed class SettingStorageSetDataFolderOperation
{
    public required IFileSystemPickerInteraction FileSystemPickerInteraction { private get; init; }

    public required IContentDialogFactory ContentDialogFactory { private get; init; }

    public required IProgressFactory ProgressFactory { private get; init; }

    public required IMessenger Messenger { get; init; }

    public async ValueTask<bool> TryExecuteAsync()
    {
        if (!FileSystemPickerInteraction.PickFolder().TryGetValue(out string? newFolderPath))
        {
            return false;
        }

        string oldFolderPath = Path.GetFullPath(HutaoRuntime.DataDirectory);
        newFolderPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(newFolderPath));
        if (UrlPath.IsEqualOrSubdirectory(oldFolderPath, newFolderPath)
            || UrlPath.IsEqualOrSubdirectory(newFolderPath, oldFolderPath))
        {
            return false;
        }

        if (Path.GetDirectoryName(newFolderPath) is null)
        {
            await ContentDialogFactory.CreateForConfirmAsync(
                    SH.ViewModelSettingStorageSetDataFolderTitle,
                    SH.ViewModelSettingStorageSetDataFolderDescription2)
                .ConfigureAwait(false);

            return false;
        }

        try
        {
            bool replaceExisting = Directory.Exists(newFolderPath) && Directory.EnumerateFileSystemEntries(newFolderPath).Any();
            if (replaceExisting && await ContentDialogFactory.CreateForConfirmCancelAsync(
                SH.ViewModelSettingStorageSetDataFolderTitle,
                SH.FormatViewModelSettingStorageMigrationReplaceConfirmation(newFolderPath)).ConfigureAwait(false) is not ContentDialogResult.Primary)
            {
                return false;
            }

            ContentDialog dialog = await ContentDialogFactory
                .CreateForIndeterminateProgressAsync(SH.ViewModelSettingStorageMigrationProgress)
                .ConfigureAwait(false);
            await ContentDialogFactory.TaskContext.SwitchToMainThreadAsync();
            DevWinUI.AnimatedToc stages = new() { IsEnabled = false };
            stages.Items.Add(new DevWinUI.AnimatedTocItem { Label = SH.ViewModelSettingStorageMigrationCompress });
            stages.Items.Add(new DevWinUI.AnimatedTocItem { Label = SH.ViewModelSettingStorageMigrationTransfer });
            stages.Items.Add(new DevWinUI.AnimatedTocItem { Label = SH.ViewModelSettingStorageMigrationExtract });
            stages.SelectedIndex = (int)UserDataMigrationStage.Compress;

            ProgressBar progressBar = new() { IsIndeterminate = true, Maximum = 100 };
            StackPanel content = new() { Spacing = 12 };
            content.Children.Add(stages);
            content.Children.Add(progressBar);
            dialog.Content = content;

            IProgress<(UserDataMigrationStage Stage, double? Percentage)> progress = ProgressFactory.CreateForMainThread<(UserDataMigrationStage Stage, double? Percentage)>(value =>
            {
                stages.SelectedIndex = (int)value.Stage;
                progressBar.IsIndeterminate = value.Percentage is null;
                progressBar.Value = value.Percentage ?? 0;
            });

            using (await ContentDialogFactory.BlockAsync(dialog).ConfigureAwait(false))
            {
                await ContentDialogFactory.TaskContext.SwitchToBackgroundAsync();
                Directory.CreateDirectory(newFolderPath);
                UserDataTransfer.CopyToDataFolder(oldFolderPath, newFolderPath, replaceExisting, progress);
                LocalSetting.Set(AppDataPaths.DataDirectorySettingKey, newFolderPath);
            }
        }
        catch (Exception ex)
        {
            Messenger.Send(InfoBarMessage.Error(ex));
            return false;
        }

        return true;
    }
}
