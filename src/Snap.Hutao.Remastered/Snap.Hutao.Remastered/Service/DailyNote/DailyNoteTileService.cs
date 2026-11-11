// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Model.Entity;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 每日便签主磁贴服务：按账户裁剪并缓存名片背景图、构建磁贴 XML、推送到开始菜单。
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IDailyNoteTileService))]
internal sealed partial class DailyNoteTileService : IDailyNoteTileService
{
    private readonly IDailyNoteTileImageCache imageCache;
    private readonly IDailyNoteTileContentFactory contentFactory;
    private readonly DailyNoteOptions options;
    private readonly ITaskContext taskContext;
    private readonly ILogger<DailyNoteTileService> logger;

    /// <summary>
    /// 最近一次发布磁贴用的条目集合，后台下载完成后用它重发磁贴。
    /// </summary>
    private IReadOnlyCollection<DailyNoteEntry>? lastPublishedEntries;

    [GeneratedConstructor]
    public partial DailyNoteTileService(IServiceProvider serviceProvider);

    /// <summary>
    /// 把磁贴背景切换到指定账户的角色：立即写设置，名片图在后台下载。
    /// 设置先行是为了不让下载阻塞切换；下载完成后 <see cref="PrepareBackgroundInBackgroundAsync"/>
    /// 会重发磁贴换上新图。
    /// </summary>
    public ValueTask<TileBackgroundSwitchResult> TrySetBackgroundAsync(string uid, string avatarId, Uri nameCardUri, CancellationToken token = default)
    {
        // 写设置前必须检查取消，否则晚完成的旧请求会覆盖新选择。
        token.ThrowIfCancellationRequested();
        options.TileBackgroundAvatarId.Value = avatarId;
        options.TileBackgroundUid.Value = uid;

        // 后台任务交回调用方观察（显示下载进度），本方法不自行启动。
        return new(new TileBackgroundSwitchResult(true, PrepareBackgroundInBackgroundAsync(avatarId, nameCardUri, token)));
    }

    /// <summary>
    /// 后台下载名片图；完成后重发磁贴让新图生效。不抛异常，
    /// 返回值区分「拿到新图」「沿用旧图」「没有背景图」与「被取消」。
    /// </summary>
    private async Task<TileBackgroundOutcome> PrepareBackgroundInBackgroundAsync(string avatarId, Uri nameCardUri, CancellationToken token)
    {
        using SentrySpanScope span = SentrySpanScope.StartOperation("tile.prepare_background", "daily_note_tile");
        span.SetData("tile.avatar_id", avatarId);

        TileBackgroundOutcome outcome;

        try
        {
            outcome = await imageCache.PrepareBackgroundAsync(avatarId, nameCardUri, token).ConfigureAwait(false)
                ? TileBackgroundOutcome.Downloaded
                : TileBackgroundOutcome.ReusedCache;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return TileBackgroundOutcome.Canceled;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Prepare tile background failed for avatar {AvatarId}", avatarId);
            outcome = TileBackgroundOutcome.Failed;
        }

        await taskContext.SwitchToMainThreadAsync();
        if (lastPublishedEntries is { } entries)
        {
            TryUpdatePrimaryTile(entries);
        }

        return outcome;
    }

    /// <summary>
    /// 清除磁贴背景设置。
    /// </summary>
    public void ClearBackground()
    {
        options.TileBackgroundAvatarId.Value = string.Empty;
        options.TileBackgroundUid.Value = string.Empty;
    }

    /// <summary>
    /// 解析指定角色已缓存的宽版背景图本地地址，没有缓存时返回 <see langword="null"/>。
    /// </summary>
    public string? ResolveCachedWideBackground(string avatarId)
    {
        return imageCache.ResolveBackgroundImageSource(avatarId, true);
    }

    /// <summary>
    /// 更新主磁贴；没有内容可发布时清空磁贴。图标缓存尽力而为地后台准备，
    /// 任何一步失败都不影响调用方的数据库写入。
    /// </summary>
    public bool TryUpdatePrimaryTile(IReadOnlyCollection<DailyNoteEntry> entries)
    {
        using SentrySpanScope span = SentrySpanScope.StartOperation("tile.update", "daily_note_tile");

        if (RuntimeEnvironment.IsUnpackaged)
        {
            return false;
        }

        // 后台下载新背景图完成后，用它重发磁贴。
        lastPublishedEntries = entries;

        // 提权进程无法更新 shell 通知；用 Environment.IsPrivilegedProcess 而不是
        // HutaoRuntime.IsProcessElevated，后者混入了 OverrideElevationRequirement 开关会误判。
        if (Environment.IsPrivilegedProcess)
        {
            return false;
        }

        imageCache.PrepareIconsIgnoringFailureAsync().SafeForget();

        try
        {
            string? tileXml = entries.Count == 0
                ? contentFactory.BuildBackgroundOnlyTileXml()
                : contentFactory.BuildTileXml(entries);

            TileUpdater updater = TileUpdateManager.CreateTileUpdaterForApplication();

            // 空内容必须用 Clear()：发送 <tile/> 不会清除上一条通知。
            if (string.IsNullOrEmpty(tileXml))
            {
                updater.Clear();
                return true;
            }

            XmlDocument document = new();
            document.LoadXml(tileXml);

            TileNotification notification = new(document);
            updater.Update(notification);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Update primary tile failed");
            return false;
        }
    }
}
