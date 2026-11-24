// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.DailyNote;

/// <summary>
/// 管理「系统计划任务定时刷新磁贴」的注册与移除。
/// <para>
/// 磁贴 API 要求进程有 MSIX 包身份，因此计划任务不能直接执行 exe：
/// 它执行一个 PowerShell 片段，通过 <c>IApplicationActivationManager</c> 以 AUMID 激活应用
/// 并传入 <c>--refresh-tile</c>，应用随即走无 UI 的刷新路径并退出。
/// </para>
/// </summary>
public interface ITileRefreshScheduler
{
    /// <summary>
    /// 查询计划任务是否已注册。异步：内部启动 schtasks 进程，绝不能阻塞 UI 线程。
    /// </summary>
    ValueTask<bool> IsRegisteredAsync(CancellationToken token = default);

    /// <summary>
    /// 读取最近一次运行结果，用于在界面上提示失败原因
    /// （脚本被拦截、WSH 被组策略禁用等）。
    /// </summary>
    ValueTask<TileRefreshScheduleStatus> GetStatusAsync(CancellationToken token = default);

    /// <summary>
    /// 创建或更新计划任务。<paramref name="intervalMinutes"/> 为触发间隔（分钟）。
    /// </summary>
    ValueTask<bool> RegisterAsync(int intervalMinutes, CancellationToken token = default);

    /// <summary>移除计划任务。</summary>
    ValueTask<bool> UnregisterAsync(CancellationToken token = default);
}