// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 磁贴图片缓存：下载远程图标与名片图，裁剪到磁贴允许的尺寸与体积，落盘到 LocalFolder，
/// 并把缓存文件解析为磁贴可以引用的本地 URI。
/// 本接口只负责文件，不读写设置项：背景选择由 <see cref="IDailyNoteTileService"/> 持久化。
/// </summary>
internal interface IDailyNoteTileImageCache
{
    /// <summary>
    /// 准备图标缓存（幂等）。失败只记录并返回，不向调用方抛出。
    /// </summary>
    Task PrepareIconsIgnoringFailureAsync();

    /// <summary>
    /// 为指定角色准备磁贴背景图（宽版 31:15 与大版 1:1）。
    /// 只做下载、裁剪与落盘，不写设置项，因此被取消的旧请求不会覆盖用户的新选择。
    /// 同一角色、同一张名片图已有完整缓存时直接复用，不访问网络；
    /// 缓存缺失或名片图地址变化时才下载。下载失败但仍有旧缓存时沿用旧图。
    /// </summary>
    ValueTask<TileBackgroundImageOutcome> PrepareBackgroundAsync(string avatarId, Uri nameCardUri, CancellationToken token);

    /// <summary>
    /// 把图标的远程地址解析为本地缓存地址；缓存不存在时回退到原始远程地址。
    /// </summary>
    string? ResolveIconSource(string url);

    /// <summary>
    /// 解析当前已应用背景图的本地地址，没有可用背景图时返回 <see langword="null"/>。
    /// </summary>
    string? ResolveBackgroundImageSource(bool isWide);

    /// <summary>
    /// 解析指定角色已缓存的背景图本地地址，没有缓存时返回 <see langword="null"/>。
    /// 供设置面板预览使用：返回的是已按磁贴比例裁切好的本地文件，
    /// 不依赖网络，呈现的也正是磁贴实际会显示的效果。
    /// </summary>
    string? ResolveBackgroundImageSource(string avatarId, bool isWide);
}

/// <summary>
/// 背景图准备的来源，用于区分「本来就有缓存」和「下载失败只能凑合」。
/// </summary>
internal enum TileBackgroundImageOutcome
{
    /// <summary>
    /// 本次下载并裁剪成功。
    /// </summary>
    Downloaded,

    /// <summary>
    /// 已有同一张名片图的缓存，直接复用，未访问网络。
    /// </summary>
    Cached,

    /// <summary>
    /// 下载失败，沿用了该角色的旧缓存。
    /// </summary>
    ReusedAfterFailure,
}
