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
    /// 为指定角色下载名片并裁剪为磁贴背景图（宽版 31:15 与大版 1:1）。
    /// 只做下载、裁剪与落盘，不写设置项，因此被取消的旧请求不会覆盖用户的新选择。
    /// 下载失败但该角色已有一份完整的旧缓存时，沿用旧图并返回 <see langword="false"/>；
    /// 用户主动取消不会被兜底。
    /// </summary>
    /// <returns>
    /// <see langword="true"/> 表示图片来自本次下载；<see langword="false"/> 表示沿用了已有缓存。
    /// </returns>
    ValueTask<bool> PrepareBackgroundAsync(string avatarId, Uri nameCardUri, CancellationToken token);

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
