// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 每日便签主磁贴服务：准备背景图并持久化背景选择，构建磁贴 XML 并推送到开始菜单。
/// <para>
/// 图片相关的工作委托给 <see cref="IDailyNoteTileImageCache"/>，
/// 磁贴 XML 的拼装委托给 <see cref="IDailyNoteTileContentFactory"/>，本接口只做编排与推送。
/// </para>
/// </summary>
internal interface IDailyNoteTileService
{
    /// <summary>
    /// 把磁贴背景切换到指定账户的角色。
    /// <para>
    /// 立即写入背景设置并返回；名片图的下载与裁剪在后台进行，
    /// 完成后由本服务重发磁贴换上新图。切换响应不再被下载阻塞，
    /// 网络慢或下载失败时磁贴沿用该角色已有的旧缓存图。
    /// </para>
    /// <para>
    /// 若请求在切换后被用户的新选择取代，本方法不再生效：
    /// <paramref name="token"/> 用于取消先发起但用户已经不想要的选择，
    /// 被取消时不会写入设置项。
    /// </para>
    /// </summary>
    ValueTask<TileBackgroundSwitchResult> TrySetBackgroundAsync(string uid, string avatarId, Uri nameCardUri, CancellationToken token = default);

    /// <summary>
    /// 清除已设置的磁贴背景。
    /// </summary>
    void ClearBackground();

    /// <summary>
    /// 解析指定角色已缓存的宽版背景图本地地址，没有缓存时返回 <see langword="null"/>。
    /// <para>
    /// 供设置面板预览使用：远程名片图既慢又会在网络抖动时加载失败，
    /// 而已缓存的本地文件正是磁贴实际显示的效果。
    /// </para>
    /// </summary>
    string? ResolveCachedWideBackground(string avatarId);

    /// <summary>
    /// 用给定的便笺条目更新主磁贴；没有内容可发布时清空磁贴。
    /// </summary>
    /// <returns>磁贴是否已更新；当前进程无法更新磁贴时返回 <see langword="false"/>。</returns>
    bool TryUpdatePrimaryTile(IReadOnlyCollection<DailyNoteEntry> entries);
}

/// <summary>
/// 磁贴背景切换的结果。
/// </summary>
/// <param name="Applied">背景设置是否已写入。</param>
/// <param name="BackgroundReady">
/// 后台下载任务：完成后给出本次背景图准备的结局。任务本身不会抛出。
/// </param>
internal readonly record struct TileBackgroundSwitchResult(bool Applied, Task<TileBackgroundOutcome> BackgroundReady);

/// <summary>
/// 后台准备磁贴背景图的结局。
/// <para>
/// 「沿用了旧图」和「彻底没有背景图」必须分开：前者的磁贴仍有背景，
/// 后者已经丢掉旧背景，提示文案与用户预期完全不同。
/// </para>
/// </summary>
internal enum TileBackgroundOutcome
{
    /// <summary>
    /// 下载并裁剪成功，磁贴换成了新图。
    /// </summary>
    Downloaded,

    /// <summary>
    /// 下载失败，但该角色此前有完整缓存，磁贴沿用旧图。
    /// </summary>
    ReusedCache,

    /// <summary>
    /// 下载失败且没有可用缓存，磁贴暂时没有背景图。
    /// </summary>
    Failed,

    /// <summary>
    /// 用户已经改选，本次准备作废（不提示）。
    /// </summary>
    Canceled,
}
