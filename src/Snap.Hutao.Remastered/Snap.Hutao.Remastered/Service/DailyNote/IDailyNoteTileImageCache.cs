// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 磁贴图片缓存：下载远程图标与名片图，裁剪到磁贴允许的尺寸与体积，落盘到 LocalFolder，
/// 并把缓存文件解析为磁贴可以引用的本地 URI。
/// <para>
/// 本接口只负责文件，不读写任何设置项：背景选择由 <see cref="IDailyNoteTileService"/> 持久化，
/// 缓存则通过读取设置项决定要解析哪个账户的背景图。
/// </para>
/// </summary>
internal interface IDailyNoteTileImageCache
{
    /// <summary>
    /// 准备图标缓存（幂等）。失败只记录 breadcrumb 并返回，不向调用方抛出。
    /// </summary>
    Task PrepareIconsIgnoringFailureAsync();

    /// <summary>
    /// 为指定角色下载名片并裁剪为磁贴背景图（宽版 31:15 与大版 1:1）。
    /// <para>
    /// 只做下载、裁剪与落盘，不写设置项，因此被取消的旧请求不会覆盖用户的新选择；
    /// UID 由调用方在持久化时写入，本方法不需要它。
    /// </para>
    /// <para>
    /// 下载失败但该角色已有一份完整的旧缓存时，沿用旧图并返回
    /// <see langword="false"/>，由调用方决定如何提示；用户主动取消不会被兜底。
    /// </para>
    /// </summary>
    /// <param name="avatarId">角色 ID，决定缓存文件名。</param>
    /// <param name="nameCardUri">名片图地址。</param>
    /// <param name="token">取消标记：用户改选后必须尽快中止下载与落盘。</param>
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
}
