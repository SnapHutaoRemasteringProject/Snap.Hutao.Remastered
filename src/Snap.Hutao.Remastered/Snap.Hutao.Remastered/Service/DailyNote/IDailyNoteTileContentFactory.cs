// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 每日便签磁贴内容工厂：把便笺条目的数据渲染成开始菜单磁贴所需的 XML。
/// <para>
/// 只负责拼装 XML，不做下载与推送；图标与背景图地址通过
/// <see cref="IDailyNoteTileImageCache"/> 解析为本地缓存地址。
/// </para>
/// </summary>
internal interface IDailyNoteTileContentFactory
{
    /// <summary>
    /// 构建包含便笺明细的磁贴 XML；没有可展示内容时返回 <see langword="null"/>。
    /// </summary>
    string? BuildTileXml(IReadOnlyCollection<DailyNoteEntry> entries);

    /// <summary>
    /// 构建只带背景图的磁贴 XML；没有背景图时返回 <see langword="null"/>，
    /// 表示没有任何内容可发布（调用方会改用 <c>TileUpdater.Clear()</c>）。
    /// </summary>
    string? BuildBackgroundOnlyTileXml();
}
