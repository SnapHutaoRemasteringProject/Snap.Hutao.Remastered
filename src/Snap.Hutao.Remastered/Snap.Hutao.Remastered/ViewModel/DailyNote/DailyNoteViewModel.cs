// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.AvatarInfo;
using Snap.Hutao.Remastered.Service.DailyNote;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
using Snap.Hutao.Remastered.UI.Xaml.View.Page;
using Snap.Hutao.Remastered.UI.Xaml.View.Window.WebView2;
using Snap.Hutao.Remastered.ViewModel.Game;
using Snap.Hutao.Remastered.ViewModel.User;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.DailyNote;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class DailyNoteViewModel : Abstraction.ViewModel, IRecipient<UserAndUidChangedMessage>
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
    private readonly ITileRefreshScheduler tileRefreshScheduler;
    private readonly IInfoBarService infoBarService;
    private readonly ILogger<DailyNoteViewModel> logger;

    private DailyNoteMetadataContext? metadataContext;

    /// <summary>
    /// 每次（重新）加载背景候选列表都换一个令牌：账户切换后仍在途的旧加载会被取消，
    /// 不会把上一个账户的角色列表覆盖到新账户上。
    /// </summary>
    private CancellationTokenSource? loadTileBackgroundCts;

    [GeneratedConstructor]
    public partial DailyNoteViewModel(IServiceProvider serviceProvider);

    public partial DailyNoteOptions DailyNoteOptions { get; }

    public partial RuntimeOptions RuntimeOptions { get; }

    public partial AppOptions AppOptions { get; }

    [ObservableProperty]
    public partial ObservableCollection<TileBackgroundAvatarItem> TileBackgroundAvatars { get; set; } = [];

    /// <summary>
    /// 全部候选角色，不受 <see cref="TileBackgroundAvatarSearchText"/> 过滤影响。
    /// </summary>
    private IReadOnlyList<TileBackgroundAvatarItem> allTileBackgroundAvatars = [];

    /// <summary>
    /// 背景候选的搜索文本；为空时显示全部角色。
    /// </summary>
    [ObservableProperty]
    public partial string? TileBackgroundAvatarSearchText { get; set; }

    [ObservableProperty]
    public partial TileBackgroundAvatarItem? SelectedTileBackgroundAvatar { get; set; }

    /// <summary>
    /// 是否已注册「后台刷新磁贴」的系统计划任务。切换时立即创建或移除任务。
    /// </summary>
    [ObservableProperty]
    public partial bool IsTileBackgroundRefreshScheduled { get; set; }

    public IJSBridgeUriSourceProvider VerifyUrlSource { get; } = new DailyJSBridgeUriSourceProvider();

    [ObservableProperty]
    public partial AdvancedDbCollectionView<User.User, Model.Entity.User>? Users { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DailyNoteEntry>? DailyNoteEntries { get; set; }

    private bool _suppressSelectionChange;

    /// <summary>
    /// 抑制「后台刷新磁贴」开关的回调，用于加载时同步真实状态、以及失败时回退开关。
    /// </summary>
    private bool _suppressTileScheduleChange;

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

            // 开关状态以系统里真实存在的计划任务为准。
            // 查询要启动子进程，必须先离开 UI 线程，否则会冻结界面。
            await taskContext.SwitchToBackgroundAsync();
            bool scheduled = await tileRefreshScheduler.IsRegisteredAsync(token).ConfigureAwait(false);
            TileRefreshScheduleStatus status = scheduled
                ? await tileRefreshScheduler.GetStatusAsync(token).ConfigureAwait(false)
                : TileRefreshScheduleStatus.NotRegistered;
            await taskContext.SwitchToMainThreadAsync();

            // 抑制回调：否则会把读到的值当成用户操作而去建/删任务。
            SetTileBackgroundRefreshScheduled(scheduled);

            // 已注册但最近一次运行失败时，直接告诉用户原因（脚本被拦截 / 脚本宿主被禁用）。
            if (status is TileRefreshScheduleStatus.ScriptMissing
                or TileRefreshScheduleStatus.ScriptHostDisabled
                or TileRefreshScheduleStatus.Failed)
            {
                messenger.Send(InfoBarMessage.Warning(TileRefreshStatusMessage(status)));
            }

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
        using SentrySpanScope span = SentrySpanScope.StartCommand(nameof(DailyNoteViewModel));

        // 换新令牌取消上一次在途加载：账户切换后旧结果必须被丢弃。
        CancellationTokenSource cts = ReplaceLoadTileBackgroundCts();
        try
        {
            await taskContext.SwitchToBackgroundAsync();
            await PrivateLoadTileBackgroundAvatarsAsync(cts).ConfigureAwait(false);

            await taskContext.SwitchToMainThreadAsync();
            ShowMsixOnlyHintOnce();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // 被新一次加载取代，属正常路径。
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Load tile background avatars failed");
            await taskContext.SwitchToMainThreadAsync();
            messenger.Send(InfoBarMessage.Error(SH.ViewModelDailyNoteTileBackgroundLoadFailed, ex));
        }
    }

    /// <summary>
    /// 首次打开背景候选时提示：磁贴功能只在 MSIX 安装包下可用，
    /// 且需要在开始菜单里固定启动器后右击磁贴调整为宽/大尺寸才能看到。
    /// 用持久化标志保证只提示一次。
    /// </summary>
    private void ShowMsixOnlyHintOnce()
    {
        if (LocalSetting.Get(SettingKeys.DailyNoteTileMsixHintShown, false))
        {
            return;
        }

        LocalSetting.Set(SettingKeys.DailyNoteTileMsixHintShown, true);
        messenger.Send(InfoBarMessage.Information(SH.ViewPageDailyNoteTileMsixOnlyHint));
    }

    /// <summary>
    /// 账户或角色切换后，背景候选列表与当前选择都需要按新账户重建。
    /// </summary>
    public void Receive(UserAndUidChangedMessage message)
    {
        if (message.UserAndUid is null)
        {
            return;
        }

        LoadTileBackgroundAvatarsAsync().SafeForget();
    }

    private CancellationTokenSource ReplaceLoadTileBackgroundCts()
    {
        CancellationTokenSource cts = new();
        CancellationTokenSource? previous = Interlocked.Exchange(ref loadTileBackgroundCts, cts);
        previous?.Cancel();
        return cts;
    }

    private async ValueTask PrivateLoadTileBackgroundAvatarsAsync(CancellationTokenSource cts)
    {
        CancellationToken token = cts.Token;

        UserAndUid? userAndUid = await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false);
        if (userAndUid is null)
        {
            return;
        }

        token.ThrowIfCancellationRequested();

        // 磁贴背景不要求账户拥有该角色，因此列出元数据里的全部角色，
        // 而不是当前账户已拥有的角色。元数据在本地，无需拉取账户数据。
        ImmutableDictionary<AvatarId, Avatar> avatars = await metadataService.GetIdToAvatarMapAsync(token).ConfigureAwait(false);

        token.ThrowIfCancellationRequested();

        // 按名字排序：中文在 zh-CN 下按拼音，其他语言按各自文化规则。
        StringComparer nameComparer = StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true);

        List<TileBackgroundAvatarItem> all = [new TileBackgroundAvatarItem(string.Empty, string.Empty, SH.ViewPageDailyNoteTileBackgroundNone, null)];

        foreach (Avatar avatar in avatars.Values.OrderBy(static avatar => avatar.Name, nameComparer))
        {
            string avatarId = avatar.Id.ToString();

            // 已下载过该角色的背景图时用它做预览：本地加载不受网络抖动影响，
            // 且呈现的就是磁贴实际显示的效果；没有缓存才退回远程名刺地址。
            Uri? cachedPreview = dailyNoteTileService.ResolveCachedWideBackground(avatarId) is { } cached
                ? new Uri(cached)
                : null;

            Uri? nameCard = AvatarNameCardPicConverter.IconNameToUri(avatar.NameCard.PicturePrefix);

            // 名片图前缀即英文/拼音名（Ayaka、Kazuha 等），作为搜索关键词。
            // Flyout 内中文输入法不可用，用户可输入拼音或英文名来过滤。
            string searchKeyword = avatar.NameCard.PicturePrefix.StartsWith("UI_NameCardPic_", StringComparison.Ordinal)
                ? avatar.NameCard.PicturePrefix["UI_NameCardPic_".Length..]
                : avatar.NameCard.PicturePrefix;

            all.Add(new TileBackgroundAvatarItem(userAndUid.Uid.Value, avatarId, avatar.Name, nameCard, cachedPreview, searchKeyword));
        }

        await taskContext.SwitchToMainThreadAsync();
        if (cts.IsCancellationRequested || cts != loadTileBackgroundCts)
        {
            // 过期加载：不能把候选列表（供搜索用）发布出去，否则会用旧账户的角色覆盖新账户。
            return;
        }

        // 通过取消与请求身份检查后才发布候选列表：
        // 防止被取消的旧账户加载污染后续搜索。
        allTileBackgroundAvatars = all;

        _suppressSelectionChange = true;
        try
        {
            // 重新加载后必须重新应用当前搜索文本：否则搜索框仍显示关键词，
            // 列表却展示了全部角色，两者不一致。
            List<TileBackgroundAvatarItem> visible = FilterBySearch(all, TileBackgroundAvatarSearchText);
            TileBackgroundAvatars = [.. visible];

            // 未设置过背景（null/空串）时默认选中「无背景」占位项；
            // 已设置背景但该角色被当前搜索隐藏时留空，让占位项保持可操作，
            // 与 RestoreSelectionToApplied 的策略一致。
            string? appliedAvatarId = DailyNoteOptions.TileBackgroundAvatarId.Value;
            SelectedTileBackgroundAvatar = string.IsNullOrEmpty(appliedAvatarId)
                ? all.FirstOrDefault(static i => i.IsNone)
                : visible.FirstOrDefault(i => i.AvatarId == appliedAvatarId);
        }
        finally
        {
            // 中途抛异常也必须复位，否则后续所有选择都会被静默忽略。
            _suppressSelectionChange = false;
        }
    }

    /// <summary>
    /// 按搜索文本从全量候选中筛出可见项；查询为空时返回全量。
    /// </summary>
    private static List<TileBackgroundAvatarItem> FilterBySearch(IReadOnlyList<TileBackgroundAvatarItem> all, string? searchText)
    {
        string? query = searchText?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            return [.. all];
        }

        List<TileBackgroundAvatarItem> filtered = [];
        foreach (TileBackgroundAvatarItem item in all)
        {
            if (item.IsNone
                || item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || (item.SearchKeyword is { } keyword && keyword.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                filtered.Add(item);
            }
        }

        return filtered;
    }

    [Command("SelectTileBackgroundCommand")]
    private async Task SelectTileBackgroundAsync(TileBackgroundAvatarItem? item)
    {
        using SentrySpanScope span = SentrySpanScope.StartCommand(nameof(DailyNoteViewModel));
        span.SetData("daily_note_tile.avatar_id", item?.AvatarId ?? "<none>");

        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Select tile background avatar", "DailyNoteViewModel.Command", [("avatarId", item?.AvatarId ?? "<null>")]));

        if (item is null)
        {
            return;
        }

        // 每次选择都取消上一个还在下载的请求，否则旧请求会在写设置时覆盖新选择。
        CancellationTokenSource cts = ReplaceTileBackgroundCts();

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
            // 立即写设置并返回，下载在服务侧后台进行，切换不再等待下载；后台任务用于显示进度。
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

            // 立即发布磁贴：有旧缓存图就直接显示新背景；没有则先显示纯内容磁贴，
            // 后台下载完成后服务会再重发一次换上新图。
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
            // 用户已改选，本次切换被主动取消。必须用 token 而不是异常类型判断：
            // HttpClient 超时抛出的 TaskCanceledException 也继承自 OperationCanceledException。
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

            messenger.Send(InfoBarMessage.Error(SH.ViewModelDailyNoteTileBackgroundFailed, ex));
        }
    }

    /// <summary>
    /// 显示背景图下载进度：先放一条常驻的转圈提示，下载结束后移除并给出结果。
    /// <c>MilliSecondsDelay</c> 为 0 的提示不会自动消失，因此直接操作
    /// <see cref="IInfoBarService.Collection"/> 管理它的生命周期。
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

        // 「直接用了已有缓存」是正常成功；「沿用旧图」与「彻底没有背景图」要分开提示：
        // 前者磁贴还有背景，后者已经丢掉旧背景。
        messenger.Send(outcome switch
        {
            TileBackgroundOutcome.Downloaded => InfoBarMessage.Success(SH.FormatViewModelDailyNoteTileBackgroundDownloadCompleted(item.Name)),
            TileBackgroundOutcome.Cached => InfoBarMessage.Success(SH.ViewModelDailyNoteTileBackgroundSet),
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

        // 清背景后把选择器复位到「无背景」项：否则界面仍显示被清的角色为选中，
        // 再次点它不会触发属性变更，用户必须先选别的才能恢复该背景。
        _suppressSelectionChange = true;
        try
        {
            SelectedTileBackgroundAvatar = TileBackgroundAvatars.FirstOrDefault(static item => item.IsNone);
        }
        finally
        {
            _suppressSelectionChange = false;
        }

        if (dailyNoteTileService.TryUpdatePrimaryTile([]))
        {
            messenger.Send(InfoBarMessage.Success(SH.ViewModelDailyNoteClearTileComplete));
        }
        else
        {
            messenger.Send(InfoBarMessage.Information(SH.ViewModelDailyNoteClearTileFailed));
        }
    }

    /// <summary>
    /// 切换「后台刷新磁贴」：注册或移除系统计划任务，使主程序未运行时磁贴也能按时更新。
    /// 由属性回调驱动而非命令：开关 TwoWay 绑定本属性，拨动时只写属性。
    /// </summary>
    partial void OnIsTileBackgroundRefreshScheduledChanged(bool value)
    {
        if (_suppressTileScheduleChange)
        {
            return;
        }

        ToggleTileBackgroundRefreshAsync(value).SafeForget();
    }

    private async Task ToggleTileBackgroundRefreshAsync(bool enable)
    {
        using SentrySpanScope span = SentrySpanScope.StartCommand(nameof(DailyNoteViewModel));
        span.SetData("daily_note.tile_schedule.enable", enable);

        if (RuntimeEnvironment.IsUnpackaged)
        {
            await taskContext.SwitchToMainThreadAsync();
            SetTileBackgroundRefreshScheduled(false);
            messenger.Send(InfoBarMessage.Warning(SH.ViewModelDailyNoteTileBackgroundRefreshRequiresMsix));
            return;
        }

        int intervalSeconds = DailyNoteOptions.SelectedRefreshTime.Value?.Value ?? 30 * 60;
        int intervalMinutes = Math.Max(1, intervalSeconds / 60);
        span.SetData("daily_note.tile_schedule.interval_minutes", intervalMinutes);

        await taskContext.SwitchToBackgroundAsync();

        // 任务注册可能跨页面存续：接 ShutdownToken，退出时中止挂起的 schtasks 操作。
        bool ok = enable
            ? await tileRefreshScheduler.RegisterAsync(intervalMinutes, XamlApplicationLifetime.ShutdownToken).ConfigureAwait(false)
            : await tileRefreshScheduler.UnregisterAsync(XamlApplicationLifetime.ShutdownToken).ConfigureAwait(false);

        await taskContext.SwitchToMainThreadAsync();

        if (ok)
        {
            messenger.Send(InfoBarMessage.Success(enable
                ? SH.FormatViewModelDailyNoteTileBackgroundRefreshScheduledInterval(intervalMinutes)
                : SH.ViewModelDailyNoteTileBackgroundRefreshUnscheduled));
            return;
        }

        // 失败时把开关退回原状，避免界面与实际任务状态不一致。
        SetTileBackgroundRefreshScheduled(!enable);
        messenger.Send(InfoBarMessage.Warning(SH.ViewModelDailyNoteTileBackgroundRefreshFailed));
    }

    /// <summary>
    /// 改开关状态但不触发注册回调。
    /// </summary>
    private void SetTileBackgroundRefreshScheduled(bool value)
    {
        _suppressTileScheduleChange = true;
        try
        {
            IsTileBackgroundRefreshScheduled = value;
        }
        finally
        {
            _suppressTileScheduleChange = false;
        }
    }

    private static string TileRefreshStatusMessage(TileRefreshScheduleStatus status)
    {
        return status switch
        {
            TileRefreshScheduleStatus.ScriptMissing => SH.ViewModelDailyNoteTileScheduleScriptMissing,
            TileRefreshScheduleStatus.ScriptHostDisabled => SH.ViewModelDailyNoteTileScheduleScriptHostDisabled,
            TileRefreshScheduleStatus.VerificationRequired => SH.ViewModelDailyNoteTileScheduleVerificationRequired,
            TileRefreshScheduleStatus.RefreshFailed => SH.ViewModelDailyNoteTileScheduleRefreshFailed,
            _ => SH.ViewModelDailyNoteTileScheduleFailed,
        };
    }

    /// <summary>
    /// 按搜索文本过滤背景候选。同时匹配中文名与英文/拼音关键词（不区分大小写）；
    /// 「无背景」项始终保留在最前，便于随时清除背景。
    /// Flyout 内中文输入法不可用，因此英文/拼音匹配是主要搜索途径。
    /// </summary>
    partial void OnTileBackgroundAvatarSearchTextChanged(string? value)
    {
        string? query = value?.Trim();

        List<TileBackgroundAvatarItem> filtered = FilterBySearch(allTileBackgroundAvatars, query);

        // 只替换列表，不主动改动选择：TwoWay 绑定在列表替换时会自行把 SelectedItem
        // 与 ItemsSource 对齐。若已应用的角色被过滤掉，UI 高亮会短暂落到占位项，
        // 但持久化设置与磁贴背景都不变；清空搜索时按持久化值恢复。
        TileBackgroundAvatars = [.. filtered];

        // 列表替换后，把高亮恢复为「已应用的角色」；它若不在过滤结果里则回落到占位项。
        RestoreSelectionToApplied(filtered);
    }

    /// <summary>
    /// 把列表选择恢复为「已应用的角色」，而不是清空用户的持久化选择。
    /// 若已应用角色被搜索过滤掉了，则留空（不选「无背景」占位项）：
    /// 占位项保持可操作，预览与磁贴都继续显示已应用的背景，
    /// 用户清空搜索后应用角色会重新出现。
    /// </summary>
    private void RestoreSelectionToApplied(List<TileBackgroundAvatarItem> filtered)
    {
        string? appliedAvatarId = DailyNoteOptions.TileBackgroundAvatarId.Value;

        // 只在应用角色可见时选中它；被隐藏时留空，让占位项保持可点击。
        TileBackgroundAvatarItem? applied = filtered.FirstOrDefault(i => i.AvatarId == appliedAvatarId);

        _suppressSelectionChange = true;
        try
        {
            SelectedTileBackgroundAvatar = applied;
        }
        finally
        {
            _suppressSelectionChange = false;
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