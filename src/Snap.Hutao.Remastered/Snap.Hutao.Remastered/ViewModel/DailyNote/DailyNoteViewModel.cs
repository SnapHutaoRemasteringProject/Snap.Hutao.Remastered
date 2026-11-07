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
    private readonly IDailyNoteTileService dailyNoteTileService;
    private readonly IInfoBarService infoBarService;
    private readonly ILogger<DailyNoteViewModel> logger;

    private DailyNoteMetadataContext? metadataContext;

    [GeneratedConstructor]
    public partial DailyNoteViewModel(IServiceProvider serviceProvider);

    public partial DailyNoteOptions DailyNoteOptions { get; }

    public partial RuntimeOptions RuntimeOptions { get; }

    public partial AppOptions AppOptions { get; }

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

    /// <summary>
    /// 当前磁贴背景选择请求；新选择到来时取消旧的，避免晚完成的下载覆盖新选择。
    /// </summary>
    private CancellationTokenSource? tileBackgroundCts;

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
            logger.LogWarning(ex, "Load tile background avatars failed");
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

        // RefreshOptionKind.None 只读数据库缓存。该账号的角色若从未在别处拉取过，
        // 缓存为空会让候选列表只剩「无背景」，因此这里补一次主动拉取。
        if (summary is null)
        {
            try
            {
                summary = await avatarInfoService.GetSummaryAsync(summaryContext, userAndUid, RefreshOptionKind.RequestFromHoyolabGameRecord).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 拉取失败（未登录、网络异常等）不影响「无背景」这一项，仅记录后继续。
                logger.LogWarning(ex, "Fetch tile background avatars failed");
            }
        }

        ObservableCollection<TileBackgroundAvatarItem> items = [];
        items.Add(new TileBackgroundAvatarItem(string.Empty, string.Empty, SH.ViewPageDailyNoteTileBackgroundNone, null));

        if (summary is not null)
        {
            foreach (AvatarView avatar in summary.Avatars)
            {
                string avatarId = avatar.Id.ToString();

                // 已下载过该角色的背景图时用它做预览：本地加载不受网络抖动影响，
                // 且呈现的就是磁贴实际显示的效果；没有缓存才退回远程名刺地址。
                Uri? cachedPreview = dailyNoteTileService.ResolveCachedWideBackground(avatarId) is { } cached
                    ? new Uri(cached)
                    : null;

                items.Add(new TileBackgroundAvatarItem(userAndUid.Uid.Value, avatarId, avatar.Name, avatar.NameCard, cachedPreview));
            }
        }

        string? selectedAvatarId = DailyNoteOptions.TileBackgroundAvatarId.Value;

        await taskContext.SwitchToMainThreadAsync();
        _suppressSelectionChange = true;
        try
        {
            TileBackgroundAvatars = items;

            // 未设置过背景时（null 或空串）默认选中「无背景」占位项。
            SelectedTileBackgroundAvatar = items.FirstOrDefault(i => i.AvatarId == selectedAvatarId) ?? items[0];
        }
        finally
        {
            // 中途抛异常也必须复位，否则后续所有选择都会被静默忽略。
            _suppressSelectionChange = false;
        }
    }

    [Command("SelectTileBackgroundCommand")]
    private async Task SelectTileBackgroundAsync(TileBackgroundAvatarItem? item)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Select tile background avatar", "DailyNoteViewModel.Command", [("avatarId", item?.AvatarId ?? "<null>")]));

        if (item is null)
        {
            return;
        }

        // 每次选择都取消上一个还在下载的请求。下载耗时可达数十秒，用户在完成前很可能
        // 又选了别的角色；如果只比对结果而不取消，旧请求仍会在 TrySetBackgroundAsync
        // 里写入设置，导致界面显示 A、磁贴却是 B。取消会让它在写设置前中止。
        CancellationTokenSource cts = ReplaceTileBackgroundCts();

        // 「无背景」占位项
        if (item.IsNone)
        {
            dailyNoteTileService.ClearBackground();

            // 清空设置后立即重发磁贴，否则旧背景图会一直留在开始菜单上
            if (DailyNoteEntries is { Count: > 0 } currentEntries)
            {
                dailyNoteTileService.TryUpdatePrimaryTile(currentEntries);
            }
            else
            {
                dailyNoteTileService.TryUpdatePrimaryTile([]);
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
            // TrySetBackgroundAsync 立即写设置并返回，下载在服务侧后台进行，
            // 因此切换不再等待下载、不再因超时失败；返回的后台任务用于显示下载进度。
            TileBackgroundSwitchResult result = await dailyNoteTileService.TrySetBackgroundAsync(item.Uid, item.AvatarId, nameCard, cts.Token).ConfigureAwait(false);

            await taskContext.SwitchToMainThreadAsync();
            if (cts.IsCancellationRequested || cts != tileBackgroundCts)
            {
                return;
            }

            if (!result.Applied)
            {
                RestoreAppliedTileBackgroundAvatar();
                messenger.Send(InfoBarMessage.Error(SH.ViewModelDailyNoteTileBackgroundFailed));
                return;
            }

            // 立即发布磁贴：新角色有旧缓存图就直接显示新背景；
            // 没有缓存则先显示纯内容磁贴，后台下载完成后服务会再重发一次换上新图。
            if (DailyNoteEntries is { Count: > 0 } entries)
            {
                dailyNoteTileService.TryUpdatePrimaryTile(entries);
            }
            else
            {
                dailyNoteTileService.TryUpdatePrimaryTile([]);
            }

            ShowBackgroundDownloadProgress(item, result.BackgroundReady, cts);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // 用户已经选了别的角色（或「无背景」），这次切换被主动取消，属于正常路径。
            // 必须用 token 判断而不是异常类型：HttpClient 超时抛出的 TaskCanceledException
            // 也继承自 OperationCanceledException，若按类型捕获，超时会被误当成「用户取消」，
            // 用户等满超时后得不到任何错误提示。
            return;
        }
        catch (Exception ex)
        {
            await taskContext.SwitchToMainThreadAsync();
            logger.LogWarning(ex, "Set tile background failed");

            // 过期请求的失败同样要丢弃，否则会把用户的新选择回滚掉。
            if (cts != tileBackgroundCts)
            {
                return;
            }

            RestoreAppliedTileBackgroundAvatar();

            // 带上异常信息，便于用户直接看到失败原因（下载失败、解码失败等）。
            messenger.Send(InfoBarMessage.Error(SH.ViewModelDailyNoteTileBackgroundFailed, ex));
        }
    }

    /// <summary>
    /// 显示背景图下载进度：先放一条常驻的转圈提示，下载结束后移除并给出结果。
    /// <para>
    /// <see cref="InfoBarMessage.MilliSecondsDelay"/> 为 0 的提示不会自动消失，
    /// 因此这里直接操作 <see cref="IInfoBarService.Collection"/> 自行管理它的生命周期；
    /// 下载完成后再发一条普通提示（会按阅读时长自动消失）。
    /// </para>
    /// </summary>
    private void ShowBackgroundDownloadProgress(TileBackgroundAvatarItem item, Task<TileBackgroundOutcome> backgroundReady, CancellationTokenSource cts)
    {
        InfoBarOptions progress = InfoBarOptions.Create(new InfoBarMessage
        {
            Severity = InfoBarSeverity.Informational,
            Message = SH.FormatViewModelDailyNoteTileBackgroundDownloading(item.Name),
            Content = new ProgressRing { IsActive = true, Width = 20, Height = 20 },
            MilliSecondsDelay = 0,
        });

        infoBarService.Collection.Insert(0, progress);
        CompleteBackgroundProgressAsync(progress, item, backgroundReady, cts).SafeForget();
    }

    /// <summary>
    /// 等待后台下载结束并收尾转圈提示。失败与取消都在这里就地处理，
    /// 因此可以安全地用 <c>SafeForget</c> 启动。
    /// </summary>
    private async Task CompleteBackgroundProgressAsync(InfoBarOptions progress, TileBackgroundAvatarItem item, Task<TileBackgroundOutcome> backgroundReady, CancellationTokenSource cts)
    {
        TileBackgroundOutcome outcome = TileBackgroundOutcome.Failed;

        try
        {
            outcome = await backgroundReady.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // 服务侧的后台任务本不抛异常；这里兜底以防未来改动，避免静默 lost task。
            logger.LogWarning(ex, "Await tile background failed");
        }

        await taskContext.SwitchToMainThreadAsync();

        // 无论是否为过期请求，都要收掉自己插入的那条转圈提示。
        infoBarService.Collection.Remove(progress);

        if (cts != tileBackgroundCts || outcome is TileBackgroundOutcome.Canceled)
        {
            // 用户已经选了别的角色，结果由那次选择负责提示。
            return;
        }

        // 「沿用了旧图」与「彻底没有背景图」必须分开提示：
        // 前者磁贴还有背景，后者已经丢掉旧背景。
        messenger.Send(outcome switch
        {
            TileBackgroundOutcome.Downloaded => InfoBarMessage.Success(SH.FormatViewModelDailyNoteTileBackgroundDownloadCompleted(item.Name)),
            TileBackgroundOutcome.ReusedCache => InfoBarMessage.Warning(SH.FormatViewModelDailyNoteTileBackgroundDownloadFallback(item.Name)),
            _ => InfoBarMessage.Warning(SH.FormatViewModelDailyNoteTileBackgroundDownloadFailed(item.Name)),
        });
    }

    /// <summary>
    /// 取消上一个背景选择请求并开启新的一个。旧的 <see cref="CancellationTokenSource"/>
    /// 只取消、不释放：被取消的请求可能仍在 <c>catch</c> 分支里读取 <c>IsCancellationRequested</c>，
    /// 提前 Dispose 会抛 <see cref="ObjectDisposedException"/>。
    /// </summary>
    private CancellationTokenSource ReplaceTileBackgroundCts()
    {
        CancellationTokenSource cts = new();
        CancellationTokenSource? previous = Interlocked.Exchange(ref tileBackgroundCts, cts);
        previous?.Cancel();
        return cts;
    }

    /// <summary>
    /// 选择变更由 ComboBox 的 TwoWay 绑定先行写入，设置失败时 <see cref="SelectedTileBackgroundAvatar"/>
    /// 已经是未生效的新项，只能按持久化中的 avatarId 还原上一次真正应用成功的角色。
    /// </summary>
    private void RestoreAppliedTileBackgroundAvatar()
    {
        string? appliedAvatarId = DailyNoteOptions.TileBackgroundAvatarId.Value;
        _suppressSelectionChange = true;

        // 与加载时一致：没有已应用的背景就回落到「无背景」占位项。
        SelectedTileBackgroundAvatar = TileBackgroundAvatars.FirstOrDefault(i => i.AvatarId == appliedAvatarId) ?? TileBackgroundAvatars.FirstOrDefault();
        _suppressSelectionChange = false;
    }

    [Command("PinTileCommand")]
    private void PinTile()
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Update daily note tile", "DailyNoteViewModel.Command"));

        if (DailyNoteEntries is { Count: > 0 } entries)
        {
            if (dailyNoteTileService.TryUpdatePrimaryTile(entries))
            {
                messenger.Send(InfoBarMessage.Success(SH.ViewModelDailyNoteUpdateTileCompleteHint));
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

        dailyNoteTileService.ClearBackground();
        if (dailyNoteTileService.TryUpdatePrimaryTile([]))
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

        if (value is not null)
        {
            // 直接调用实现方法，避免 IAsyncRelayCommand<T>.CanExecute(object) 的泛型参数转换失败。
            SelectTileBackgroundAsync(value).SafeForget();
        }
    }
}