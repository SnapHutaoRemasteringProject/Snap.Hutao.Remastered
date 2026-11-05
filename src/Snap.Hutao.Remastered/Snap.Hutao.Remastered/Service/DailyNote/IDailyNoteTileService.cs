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
    /// 为指定账户设置磁贴背景：下载并裁剪名片图，成功后写入背景设置。
    /// <para>
    /// 若请求在下载或落盘期间被取消，本方法不会写入设置项，并抛出
    /// <see cref="OperationCanceledException"/>。
    /// </para>
    /// </summary>
    /// <returns>背景是否已设置成功。</returns>
    ValueTask<bool> TrySetBackgroundAsync(string uid, string avatarId, Uri nameCardUri, CancellationToken token = default);

    /// <summary>
    /// 清除已设置的磁贴背景。
    /// </summary>
    void ClearBackground();

    /// <summary>
    /// 用给定的便笺条目更新主磁贴；没有内容可发布时清空磁贴。
    /// </summary>
    /// <returns>磁贴是否已更新；当前进程无法更新磁贴时返回 <see langword="false"/>。</returns>
    bool TryUpdatePrimaryTile(IReadOnlyCollection<DailyNoteEntry> entries);
}
