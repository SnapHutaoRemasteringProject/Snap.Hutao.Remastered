// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 每日便签主磁贴服务：准备背景图并持久化背景选择，构建磁贴 XML 并推送到开始菜单。
/// 图片相关工作委托给 <see cref="IDailyNoteTileImageCache"/>，XML 拼装委托给
/// <see cref="IDailyNoteTileContentFactory"/>，本接口只做编排与推送。
/// </summary>
internal interface IDailyNoteTileService
{
    /// <summary>
    /// 把磁贴背景切换到指定账户的角色：立即写设置，名片图在后台下载。
    /// 下载完成后由本服务重发磁贴换上新图；网络慢或下载失败时沿用该角色的旧缓存图。
    /// 若切换后用户又选了别的角色，<paramref name="token"/> 被取消，本方法不再写设置。
    /// </summary>
    ValueTask<TileBackgroundSwitchResult> TrySetBackgroundAsync(string uid, string avatarId, Uri nameCardUri, CancellationToken token = default);

    /// <summary>
    /// 清除已设置的磁贴背景。
    /// </summary>
    void ClearBackground();

    /// <summary>
    /// 解析指定角色已缓存的宽版背景图本地地址，没有缓存时返回 <see langword="null"/>。
    /// 供设置面板预览使用：本地文件不受网络抖动影响，且正是磁贴实际显示的效果。
    /// </summary>
    string? ResolveCachedWideBackground(string avatarId);

    /// <summary>
    /// 用给定的便笺条目更新主磁贴；没有内容可发布时清空磁贴。
    /// </summary>
    /// <returns>更新结果。调用方据此给出可区分的提示，而不是笼统的「更新失败」。</returns>
    TileUpdateResult UpdatePrimaryTile(IReadOnlyCollection<DailyNoteEntry> entries);
}

/// <summary>
/// 一次主磁贴更新的结果。原先只返回 <see langword="bool"/>，界面无法区分
/// 「环境不支持」「没有内容」「写入被系统拒绝」这些完全不同的情况。
/// </summary>
internal enum TileUpdateOutcome
{
    /// <summary>磁贴已按当前数据更新（或按无内容清空）。</summary>
    Succeeded,

    /// <summary>未打包运行：进程没有包身份，磁贴 API 不可用。</summary>
    NotPackaged,

    /// <summary>进程已提权：Shell 通知由未提权的资源管理器接管，提权进程写不进去。</summary>
    Elevated,

    /// <summary>磁贴 XML 已生成，但系统拒绝写入（配额、格式或通知服务异常）。</summary>
    UpdateRejected,

    /// <summary>生成磁贴内容时出错（模板或图片地址不可用）。</summary>
    ContentFailed,
}

/// <summary>
/// 磁贴更新结果与失败原因。<see cref="Failure"/> 只用于日志与诊断，
/// 不直接显示给用户（界面按 <see cref="Outcome"/> 取本地化文案）。
/// </summary>
/// <param name="Outcome">结果分类。</param>
/// <param name="Failure">失败时记录的原因；成功时为 <see langword="null"/>。</param>
internal sealed record TileUpdateResult(TileUpdateOutcome Outcome, string? Failure = null)
{
    public bool Succeeded => Outcome is TileUpdateOutcome.Succeeded;
}

/// <summary>
/// 磁贴背景切换的结果。
/// </summary>
/// <param name="Applied">背景设置是否已写入。</param>
/// <param name="BackgroundReady">后台下载任务：完成后给出本次背景图准备的结局。任务本身不会抛出。</param>
internal readonly record struct TileBackgroundSwitchResult(bool Applied, Task<TileBackgroundOutcome> BackgroundReady);

/// <summary>
/// 后台准备磁贴背景图的结局。「沿用了旧图」和「彻底没有背景图」必须分开：
/// 前者的磁贴仍有背景，后者已经丢掉旧背景。
/// </summary>
internal enum TileBackgroundOutcome
{
    /// <summary>
    /// 下载并裁剪成功，磁贴换成了新图。
    /// </summary>
    Downloaded,

    /// <summary>
    /// 同一张名片图已有缓存，直接复用，未访问网络。
    /// </summary>
    Cached,

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
