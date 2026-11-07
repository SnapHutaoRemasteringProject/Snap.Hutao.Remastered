// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
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
    /// 最近一次发布磁贴用的条目集合。后台下载完成后用它重发磁贴，
    /// 让新落盘的背景图立即生效，而不是等下一次便笺刷新。
    /// </summary>
    private IReadOnlyCollection<DailyNoteEntry>? lastPublishedEntries;

    [GeneratedConstructor]
    public partial DailyNoteTileService(IServiceProvider serviceProvider);

    /// <summary>
    /// 把磁贴背景切换到指定账户的角色：立即写设置，后台下载图。
    /// <para>
    /// 之前是先下载、成功后才写设置——名片图约 600KB，网络差时下载要数十秒甚至超时，
    /// 用户切换角色后磁贴迟迟不响应。现在设置先行：
    /// 磁贴立即指向新角色（该角色有旧缓存图就直接显示，没有则显示纯内容磁贴），
    /// 下载在后台完成后重发一次磁贴，换上新图。
    /// </para>
    /// <para>
    /// <paramref name="token"/> 用于取消先发起但用户已经不想要的选择：
    /// 写入设置之前必须检查，否则晚完成的旧请求会把新选择覆盖掉。
    /// </para>
    /// <para>
    /// 返回的后台任务供调用方显示下载进度；本方法自己不启动它，
    /// 避免同一次下载被 SafeForget 与调用方各观察一次。
    /// </para>
    /// </summary>
    public ValueTask<TileBackgroundSwitchResult> TrySetBackgroundAsync(string uid, string avatarId, Uri nameCardUri, CancellationToken token = default)
    {
        // 第一步：写设置（取消检查必须在写之前，否则旧请求会覆盖新选择）。
        token.ThrowIfCancellationRequested();
        options.TileBackgroundAvatarId.Value = avatarId;
        options.TileBackgroundUid.Value = uid;

        // 第二步：把后台下载交给调用方观察——调用方需要它来显示进度。
        // 本方法自身立即完成：同步路径不该带 async，否则只会多一层状态机。
        return new(new TileBackgroundSwitchResult(true, PrepareBackgroundInBackgroundAsync(avatarId, nameCardUri, token)));
    }

    /// <summary>
    /// 后台下载名片图；完成后切回主线程重发一次磁贴，让新图生效。
    /// 失败时沿用该角色的旧缓存（<see cref="IDailyNoteTileImageCache"/> 内部处理），
    /// 最坏情况是磁贴继续显示旧图，不影响已经生效的设置。
    /// <para>
    /// 本方法不抛异常，返回值区分「拿到新图」「沿用旧图」与「彻底没有背景图」，
    /// 调用方据此给出不同的提示。取消单独返回，交由调用方忽略。
    /// </para>
    /// </summary>
    private async Task<TileBackgroundOutcome> PrepareBackgroundInBackgroundAsync(string avatarId, Uri nameCardUri, CancellationToken token)
    {
        TileBackgroundOutcome outcome;

        try
        {
            outcome = await imageCache.PrepareBackgroundAsync(avatarId, nameCardUri, token).ConfigureAwait(false)
                ? TileBackgroundOutcome.Downloaded
                : TileBackgroundOutcome.ReusedCache;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // 用户已经选了别的角色，后台下载被取消，属正常路径。
            return TileBackgroundOutcome.Canceled;
        }
        catch (Exception ex)
        {
            // 下载失败且没有可沿用的缓存：设置已经生效，但磁贴暂时拿不到背景图。
            logger.LogWarning(ex, "Prepare tile background failed for avatar {AvatarId}", avatarId);
            outcome = TileBackgroundOutcome.Failed;
        }

        // 新图落盘（或确认无图可用）后重发磁贴，让磁贴与当前设置一致。
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
    /// 更新主磁贴：图标缓存尽力而为地后台准备，任何一步失败都不影响调用方的数据库写入。
    /// </summary>
    public bool TryUpdatePrimaryTile(IReadOnlyCollection<DailyNoteEntry> entries)
    {
        if (RuntimeEnvironment.IsUnpackaged)
        {
            return false;
        }

        // 记住最近发布的集合：后台下载新背景图完成后，用它重发磁贴让新图立即生效。
        lastPublishedEntries = entries;

        // 提权进程无法更新 shell 通知（见 ToastNotificationService 的非提权辅助进程方案）。
        // 主路径下提权实例由 runas 启动、不带包标识，上面的 IsUnpackaged 已拦住；
        // 这里再显式挡一次，覆盖 "已打包但提权" 的边缘情况。
        // 用 Environment.IsPrivilegedProcess 而不是 HutaoRuntime.IsProcessElevated：
        // 后者还混入了 OverrideElevationRequirement 设置开关，普通打包进程打开该开关后
        // 会被误判为提权，导致磁贴无法更新或清除。
        if (Environment.IsPrivilegedProcess)
        {
            return false;
        }

        // 图标缓存是尽力而为的后台准备，失败已在 helper 内记录，不阻塞磁贴更新。
        imageCache.PrepareIconsIgnoringFailureAsync().SafeForget();

        // 磁贴更新只是锦上添花，任何一步失败都不应该让调用方的数据库写入回滚或抛出。
        try
        {
            string? tileXml = entries.Count == 0
                ? contentFactory.BuildBackgroundOnlyTileXml()
                : contentFactory.BuildTileXml(entries);

            TileUpdater updater = TileUpdateManager.CreateTileUpdaterForApplication();

            // 没有任何内容可发布时必须用 Clear()：发送 <tile/> 没有内容绑定，
            // Windows 不会清除上一条通知，旧内容会一直留在磁贴上。
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
