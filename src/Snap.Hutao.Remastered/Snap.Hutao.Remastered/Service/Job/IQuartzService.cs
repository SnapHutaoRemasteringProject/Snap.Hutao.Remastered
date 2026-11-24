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

    Task UpdateJobAsync(string group, string triggerName, Func<TriggerBuilder<IJob>, TriggerBuilder<IJob>> configure, CancellationToken token = default);

    /// <summary>
    /// 确保指定 Job 已注册：不存在则创建并排程，存在则按 <paramref name="triggerFactory"/>
    /// 重建触发器（间隔可能已变化）。用于「运行中打开自动刷新」这类启动时未注册的场景。
    /// </summary>
    Task EnsureJobAsync(string group, string triggerName, Func<IJobDetail> jobFactory, Func<JobKey, ITrigger> triggerFactory, CancellationToken token = default);
}
