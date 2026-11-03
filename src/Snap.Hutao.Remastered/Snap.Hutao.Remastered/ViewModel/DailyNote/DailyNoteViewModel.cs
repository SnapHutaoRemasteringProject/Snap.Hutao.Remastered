// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.AvatarInfo;
using Snap.Hutao.Remastered.Service.AvatarInfo.Factory;
using Snap.Hutao.Remastered.Service.DailyNote;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
using Snap.Hutao.Remastered.UI.Xaml.View.Page;
using Snap.Hutao.Remastered.UI.Xaml.View.Window.WebView2;
using Snap.Hutao.Remastered.ViewModel.AvatarProperty;
using Snap.Hutao.Remastered.ViewModel.Game;
using Snap.Hutao.Remastered.ViewModel.User;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.ViewModel.DailyNote;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class DailyNoteViewModel : Abstraction.ViewModel
{
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly INavigationService navigationService;
    private readonly IDailyNoteService dailyNoteService;
    private readonly IServiceProvider serviceProvider;
    private readonly IMetadataService metadataService;
    private readonly ITaskContext taskContext;
    private readonly IUserService userService;
    private readonly IMessenger messenger;
    private readonly IAvatarInfoService avatarInfoService;

    private DailyNoteMetadataContext? metadataContext;

    [GeneratedConstructor]
    public partial DailyNoteViewModel(IServiceProvider serviceProvider);

    public partial DailyNoteOptions DailyNoteOptions { get; }

    public partial RuntimeOptions RuntimeOptions { get; }

    public partial AppOptions AppOptions { get; }

    private partial DailyNoteTileOperation DailyNoteTileOperation { get; }

    [ObservableProperty]
    public partial ObservableCollection<TileBackgroundAvatarItem> TileBackgroundAvatars { get; set; } = [];

    [ObservableProperty]
    public partial TileBackgroundAvatarItem? SelectedTileBackgroundAvatar { get; set; }

    public IJSBridgeUriSourceProvider VerifyUrlSource { get; } = new DailyJSBridgeUriSourceProvider();

    [ObservableProperty]
    public partial AdvancedDbCollectionView<User.User, Model.Entity.User>? Users { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DailyNoteEntry>? DailyNoteEntries { get; set; }

    private bool _suppressSelectionChange;

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        if (!await metadataService.InitializeAsync().ConfigureAwait(false))
        {
            return false;
        }

        metadataContext = await metadataService.GetContextAsync<DailyNoteMetadataContext>(token).ConfigureAwait(false);

        try
        {
            await taskContext.SwitchToBackgroundAsync();
            AdvancedDbCollectionView<User.User, Model.Entity.User> users = await userService.GetUsersAsync().ConfigureAwait(false);
            ObservableCollection<DailyNoteEntry> entries = await dailyNoteService.GetDailyNoteEntryCollectionAsync(metadataContext, false, token).ConfigureAwait(false);

            await taskContext.SwitchToMainThreadAsync();
            Users = users;
            DailyNoteEntries = entries;

            LoadTileBackgroundAvatarsAsync().SafeForget();
            return true;
        }
        catch (HutaoException ex)
        {
            messenger.Send(InfoBarMessage.Error(ex));
        }

        return false;
    }

    [Command("TrackCurrentUserAndUidCommand")]
    private async Task TrackCurrentUserAndUidAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Add daily note", "DailyNoteViewModel.Command"));

        if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is not { } userAndUid)
        {
            messenger.Send(InfoBarMessage.Warning(SH.MustSelectUserAndUid));
            return;
        }

        ArgumentNullException.ThrowIfNull(metadataContext);

        ContentDialog dialog = await contentDialogFactory
            .CreateForIndeterminateProgressAsync(SH.ViewModelDailyNoteRequestProgressTitle)
            .ConfigureAwait(false);

        using (await contentDialogFactory.BlockAsync(dialog).ConfigureAwait(false))
        {
            await dailyNoteService.AddDailyNoteAsync(metadataContext, userAndUid).ConfigureAwait(false);
        }
    }

    [Command("RefreshCommand")]
    private async Task RefreshAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Refresh daily note", "DailyNoteViewModel.Command"));
        await dailyNoteService.RefreshDailyNotesAsync().ConfigureAwait(false);
    }

    [Command("StartGameFromDailyNoteCommand")]
    private async Task StartGameFromDailyNoteAsync(DailyNoteEntry? entry)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Start game", "DailyNoteViewModel.Command", [("uid", entry?.Uid ?? "<null>")]));

        if (entry is not null)
        {
            await navigationService
                .NavigateAsync<LaunchGamePage>(LaunchGameExtraData.CreateForUid(entry.Uid), true)
                .ConfigureAwait(false);
        }
    }

    [Command("RemoveDailyNoteCommand")]
    private async Task RemoveDailyNoteAsync(DailyNoteEntry? entry)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Remove daily note", "DailyNoteViewModel.Command", [("uid", entry?.Uid ?? "<null>")]));

        if (entry is not null)
        {
            await dailyNoteService.RemoveDailyNoteAsync(entry).ConfigureAwait(false);
        }
    }

    [Command("ModifyNotificationCommand")]
    private async Task ModifyDailyNoteNotificationAsync(DailyNoteEntry? entry)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Modify daily note notification settings", "DailyNoteViewModel.Command", [("uid", entry?.Uid ?? "<null>")]));

        if (entry is not null)
        {
            using (await EnterCriticalSectionAsync().ConfigureAwait(false))
            {
                DailyNoteNotificationDialog dialog = await contentDialogFactory.CreateInstanceAsync<DailyNoteNotificationDialog>(serviceProvider, entry).ConfigureAwait(true);
                await contentDialogFactory.EnqueueAndShowAsync(dialog).ShowTask.ConfigureAwait(false);

                await taskContext.SwitchToBackgroundAsync();
                await dailyNoteService.UpdateDailyNoteAsync(entry).ConfigureAwait(false);
            }
        }
    }

    [Command("ConfigDailyNoteWebhookUrlCommand")]
    private async Task ConfigDailyNoteWebhookUrlAsync()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Modify daily note webhook settings", "DailyNoteViewModel.Command"));

        DailyNoteWebhookDialog dialog = await contentDialogFactory.CreateInstanceAsync<DailyNoteWebhookDialog>(serviceProvider).ConfigureAwait(true);
        dialog.Text = DailyNoteOptions.WebhookUrl.Value;
        (bool isOk, string? url) = await dialog.GetInputUrlAsync().ConfigureAwait(false);

        if (isOk)
        {
            await taskContext.SwitchToMainThreadAsync();
            DailyNoteOptions.WebhookUrl.Value = url;
            messenger.Send(InfoBarMessage.Information(SH.ViewModelDailyNoteConfigWebhookUrlComplete));
        }
    }

    [Command("LoadTileBackgroundAvatarsCommand")]
    private async Task LoadTileBackgroundAvatarsAsync()
    {
        await taskContext.SwitchToBackgroundAsync();
        try
        {
            await PrivateLoadTileBackgroundAvatarsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Load tile background avatars failed", "DailyNoteViewModel", [("error", ex.GetType().Name)]));
            await taskContext.SwitchToMainThreadAsync();
            messenger.Send(InfoBarMessage.Error(SH.ViewModelDailyNoteTileBackgroundLoadFailed, ex));
        }
    }

    private async ValueTask PrivateLoadTileBackgroundAvatarsAsync()
    {
        UserAndUid? userAndUid = await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false);
        if (userAndUid is null)
        {
            return;
        }

        SummaryFactoryMetadataContext summaryContext = await metadataService.GetContextAsync<SummaryFactoryMetadataContext>().ConfigureAwait(false);
        Summary? summary = await avatarInfoService.GetSummaryAsync(summaryContext, userAndUid, RefreshOptionKind.None).ConfigureAwait(false);

        ObservableCollection<TileBackgroundAvatarItem> items = [];
        items.Add(new TileBackgroundAvatarItem(string.Empty, string.Empty, SH.ViewPageDailyNoteTileBackgroundNone, null));

        if (summary is not null)
        {
            foreach (AvatarView avatar in summary.Avatars)
            {
                items.Add(new TileBackgroundAvatarItem(userAndUid.Uid.Value, avatar.Id.ToString(), avatar.Name, avatar.NameCard));
            }
        }

        string? selectedAvatarId = DailyNoteOptions.TileBackgroundAvatarId.Value;
        await taskContext.SwitchToMainThreadAsync();
        _suppressSelectionChange = true;
        TileBackgroundAvatars = items;
        SelectedTileBackgroundAvatar = items.FirstOrDefault(i => i.AvatarId == selectedAvatarId);
        _suppressSelectionChange = false;
    }

    [Command("SelectTileBackgroundCommand")]
    private async Task SelectTileBackgroundAsync(TileBackgroundAvatarItem? item)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Select tile background avatar", "DailyNoteViewModel.Command", [("avatarId", item?.AvatarId ?? "<null>")]));

        if (item is null)
        {
            return;
        }

        // 「无背景」占位项
        if (item.IsNone)
        {
            DailyNoteTileOperation.ClearBackground();

            // 清空设置后立即重发磁贴，否则旧背景图会一直留在开始菜单上
            if (DailyNoteEntries is { Count: > 0 } currentEntries)
            {
                DailyNoteTileOperation.TryUpdatePrimaryTile(currentEntries);
            }
            else
            {
                DailyNoteTileOperation.TryUpdatePrimaryTile([]);
            }
            messenger.Send(InfoBarMessage.Success(SH.ViewModelDailyNoteTileBackgroundCleared));
            return;
        }

        if (item.NameCard is not { } nameCard)
        {
            messenger.Send(InfoBarMessage.Error(SH.ViewModelDailyNoteTileBackgroundFailed));
            return;
        }

        try
        {
            bool ok = await DailyNoteTileOperation.TrySetBackgroundAsync(item.Uid, item.AvatarId, nameCard).ConfigureAwait(false);

            await taskContext.SwitchToMainThreadAsync();
            if (ok)
            {
                // 无论便笺条目是否为空都要发布磁贴：空条目时发布纯背景磁贴，
                // 否则刚保存的背景图不会显示。
                if (DailyNoteEntries is { Count: > 0 } entries)
                {
                    DailyNoteTileOperation.TryUpdatePrimaryTile(entries);
                }
                else
                {
                    DailyNoteTileOperation.TryUpdatePrimaryTile([]);
                }

                messenger.Send(InfoBarMessage.Success(SH.ViewModelDailyNoteTileBackgroundSet));
            }
            else
            {
                RestoreAppliedTileBackgroundAvatar();
                messenger.Send(InfoBarMessage.Error(SH.ViewModelDailyNoteTileBackgroundFailed));
            }
        }
        catch
        {
            await taskContext.SwitchToMainThreadAsync();
            RestoreAppliedTileBackgroundAvatar();
            messenger.Send(InfoBarMessage.Error(SH.ViewModelDailyNoteTileBackgroundFailed));
        }
    }

    /// <summary>
    /// 选择变更由 ComboBox 的 TwoWay 绑定先行写入，设置失败时 <see cref="SelectedTileBackgroundAvatar"/>
    /// 已经是未生效的新项，只能按持久化中的 avatarId 还原上一次真正应用成功的角色。
    /// </summary>
    private void RestoreAppliedTileBackgroundAvatar()
    {
        string appliedAvatarId = DailyNoteOptions.TileBackgroundAvatarId.Value;
        _suppressSelectionChange = true;
        SelectedTileBackgroundAvatar = TileBackgroundAvatars.FirstOrDefault(i => i.AvatarId == appliedAvatarId);
        _suppressSelectionChange = false;
    }

    [Command("PinTileCommand")]
    private void PinTile()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Update daily note tile", "DailyNoteViewModel.Command"));

        if (DailyNoteEntries is { Count: > 0 } entries)
        {
            if (DailyNoteTileOperation.TryUpdatePrimaryTile(entries))
            {
                messenger.Send(InfoBarMessage.Success(SH.ViewModelDailyNoteUpdateTileComplete));
            }
            else
            {
                messenger.Send(InfoBarMessage.Information(SH.ViewModelDailyNoteUpdateTileFailed));
            }
        }
        else
        {
            messenger.Send(InfoBarMessage.Information(SH.ViewModelDailyNoteUpdateTileFailed));
        }
    }

    [Command("UnpinTileCommand")]
    private void UnpinTile()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Clear daily note tile", "DailyNoteViewModel.Command"));

        DailyNoteTileOperation.ClearBackground();
        if (DailyNoteTileOperation.TryUpdatePrimaryTile([]))
        {
            messenger.Send(InfoBarMessage.Success(SH.ViewModelDailyNoteClearTileComplete));
        }
        else
        {
            messenger.Send(InfoBarMessage.Information(SH.ViewModelDailyNoteClearTileFailed));
        }
    }

    partial void OnSelectedTileBackgroundAvatarChanged(TileBackgroundAvatarItem? value)
    {
        if (_suppressSelectionChange)
        {
            return;
        }

        if (value is not null && SelectTileBackgroundCommand.CanExecute(value))
        {
            SelectTileBackgroundCommand.Execute(value);
        }
    }
}