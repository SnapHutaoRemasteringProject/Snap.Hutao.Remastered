// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Quartz;

namespace Snap.Hutao.Remastered.Service.Job;

[SuppressMessage("", "SH003")]
public interface IQuartzService
{
    Task StartAsync(CancellationToken token = default);

    Task ShutdownAsync(CancellationToken token = default);

    Task StopJobAsync(string group, string triggerName, CancellationToken token = default);

    /// <summary>
    /// 停止指定 Job，但与注册走同一队列：不会出现「排队的注册在关闭之后又把触发器建回来」。
    /// </summary>
    Task StopJobSerializedAsync(string group, string triggerName, CancellationToken token = default);

    Task UpdateJobAsync(string group, string triggerName, Func<TriggerBuilder<IJob>, TriggerBuilder<IJob>> configure, CancellationToken token = default);

    /// <summary>
    /// 确保指定 Job 已注册：不存在则创建并排程，存在则按 <paramref name="triggerFactory"/>
    /// 重建触发器（间隔可能已变化）。用于「运行中打开自动刷新」这类启动时未注册的场景。
    /// </summary>
    /// <param name="group">Job 与触发器的分组。</param>
    /// <param name="triggerName">触发器名，与 <paramref name="group"/> 一起唯一标识调度项。</param>
    /// <param name="jobFactory">触发器不存在时用来创建 Job。</param>
    /// <param name="triggerFactory">按 JobKey 构造触发器。</param>
    /// <param name="shouldApply">
    /// 取得队列后重新确认是否仍需要注册。排队期间用户可能已经关掉了开关，
    /// 返回 <see langword="false"/> 时放弃本次注册。
    /// </param>
    /// <param name="token">取消令牌。</param>
    Task EnsureJobAsync(string group, string triggerName, Func<IJobDetail> jobFactory, Func<JobKey, ITrigger> triggerFactory, Func<bool>? shouldApply = null, CancellationToken token = default);
}
