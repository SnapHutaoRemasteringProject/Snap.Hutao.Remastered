// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Quartz;

namespace Snap.Hutao.Remastered.Service.Job;

[Service(ServiceLifetime.Singleton, typeof(IQuartzService))]
[SuppressMessage("", "SH003")]
public sealed partial class QuartzService : IQuartzService, IDisposable
{
    private readonly TaskCompletionSource startupCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource lifetime = new();
    private readonly object gate = new();

    /// <summary>
    /// 串行化 Job 注册：启动阶段的 <see cref="ConfigureJobsAsync"/> 与运行时开关触发的
    /// <see cref="EnsureJobAsync"/> 可能重叠。两者都会先查 Trigger 再按需 Schedule，
    /// 交错时后一个会撞上 <c>ObjectAlreadyExistsException</c>，或者让启动时读到的旧间隔
    /// 覆盖用户刚选的新间隔。串行执行后「后发生的调用生效」。
    /// </summary>
    private readonly SemaphoreSlim jobGate = new(1, 1);

    private readonly ISchedulerFactory schedulerFactory;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<QuartzService> logger;
    private IScheduler? scheduler;
    private Task? startTask;
    private Task? configureTask;
    private Task? shutdownTask;

    [GeneratedConstructor]
    public partial QuartzService(IServiceProvider serviceProvider);

    public Task StartAsync(CancellationToken token = default)
    {
        lock (gate) return startTask ??= StartCoreAsync(token);
    }

    private async Task StartCoreAsync(CancellationToken token)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
        try
        {
            scheduler = await schedulerFactory.GetScheduler(linked.Token).ConfigureAwait(false);
            linked.Token.ThrowIfCancellationRequested();
            await scheduler.Start(linked.Token).ConfigureAwait(false);
            startupCompleted.TrySetResult();
            // Job registration may await user/metadata initialization in activation
            // phase 2. It must not prevent phase 1 from ever completing.
            configureTask = ConfigureJobsAsync(scheduler, lifetime.Token);
            configureTask.SafeForget();
        }
        finally
        {
            startupCompleted.TrySetResult();
        }
    }

    private async Task ConfigureJobsAsync(IScheduler current, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            await jobGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                foreach (IJobScheduler jobScheduler in serviceProvider.GetServices<IJobScheduler>())
                {
                    token.ThrowIfCancellationRequested();
                    await jobScheduler.ScheduleAsync(current, token).ConfigureAwait(false);
                }
            }
            finally
            {
                jobGate.Release();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    public async Task UpdateJobAsync(string group, string triggerName, Func<TriggerBuilder<IJob>, TriggerBuilder<IJob>> configure, CancellationToken token = default)
    {
        await startupCompleted.Task.WaitAsync(token).ConfigureAwait(false);
        if (scheduler is not { } current || lifetime.IsCancellationRequested) return;
        TriggerKey key = new(triggerName, group);
        if (await current.GetTrigger(key, token).ConfigureAwait(false) is { } old)
            await current.RescheduleJob(key, configure(old.GetTriggerBuilder()).Build(), token).ConfigureAwait(false);
    }

    public async Task EnsureJobAsync(string group, string triggerName, Func<IJobDetail> jobFactory, Func<JobKey, ITrigger> triggerFactory, Func<bool>? shouldApply = null, CancellationToken token = default)
    {
        await startupCompleted.Task.WaitAsync(token).ConfigureAwait(false);
        if (scheduler is not { } current || lifetime.IsCancellationRequested) return;

        // 与 ConfigureJobsAsync / StopJobSerializedAsync 串行：避免同一 TriggerKey 被并发
        // Schedule，也保证「关闭」不会被排队中的「开启」重新建回来。
        await jobGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            // 排队期间用户可能已经关掉开关：此时不能再创建或重建触发器。
            if (shouldApply is not null && !shouldApply())
            {
                return;
            }

            TriggerKey key = new(triggerName, group);
            if (await current.GetTrigger(key, token).ConfigureAwait(false) is { } existing)
            {
                // Job 已注册：只重建触发器，覆盖可能变化的间隔。触发器必须挂到既有 JobKey 上。
                await current.RescheduleJob(key, triggerFactory(existing.JobKey), token).ConfigureAwait(false);
            }
            else
            {
                // Job 未注册（启动时开关是关的，之后才打开）：首次创建并排程。
                IJobDetail job = jobFactory();
                await current.ScheduleJob(job, triggerFactory(job.Key), cancellationToken: token).ConfigureAwait(false);
            }
        }
        finally
        {
            jobGate.Release();
        }
    }

    public async Task StopJobAsync(string group, string triggerName, CancellationToken token = default)
    {
        await startupCompleted.Task.WaitAsync(token).ConfigureAwait(false);
        if (scheduler is { } current && !lifetime.IsCancellationRequested)
            await current.UnscheduleJob(new(triggerName, group), token).ConfigureAwait(false);
    }

    public async Task StopJobSerializedAsync(string group, string triggerName, CancellationToken token = default)
    {
        await startupCompleted.Task.WaitAsync(token).ConfigureAwait(false);
        if (scheduler is not { } current || lifetime.IsCancellationRequested) return;

        // 与注册共用队列：排队中的 EnsureJobAsync 会先执行完，随后这里把它撤掉，
        // 不会出现「撤销后又冒出一个触发器」。
        await jobGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await current.UnscheduleJob(new(triggerName, group), token).ConfigureAwait(false);
        }
        finally
        {
            jobGate.Release();
        }
    }

    public Task ShutdownAsync(CancellationToken token = default)
    {
        lock (gate) return shutdownTask ??= Task.Run(() => ShutdownCoreAsync(token));
    }

    private async Task ShutdownCoreAsync(CancellationToken token)
    {
        lifetime.Cancel();
        startupCompleted.TrySetResult();
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            Task? starting;
            lock (gate) starting = startTask;
            if (starting is not null)
            {
                try { await starting.WaitAsync(timeout.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (!timeout.IsCancellationRequested) { }
                catch (Exception exception) when (!timeout.IsCancellationRequested)
                {
                    logger.LogWarning(exception, "Quartz startup failed before shutdown");
                }
            }
            if (scheduler is not { } current) return;
            try
            {
                if (configureTask is { } configuring)
                {
                    try { await configuring.WaitAsync(timeout.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (!timeout.IsCancellationRequested) { }
                    catch (Exception exception) when (!timeout.IsCancellationRequested)
                    {
                        logger.LogWarning(exception, "Quartz job registration stopped during shutdown");
                    }
                }
                await current.Interrupt(new(JobIdentity.DailyNoteRefreshJobName, JobIdentity.DailyNoteGroupName), timeout.Token).ConfigureAwait(false);
                await current.Interrupt(new(JobIdentity.AutoSignInRunJobName, JobIdentity.AutoSignInGroupName), timeout.Token).ConfigureAwait(false);
            }
            finally
            {
                // Start shutdown even when job registration exhausted the wait
                // budget. Waiting remains bounded by the shared deadline.
                Task stopping = StopSchedulerAsync(current);
                stopping.SafeForget();
                await stopping.WaitAsync(timeout.Token).ConfigureAwait(false);
                scheduler = null;
            }
        }
        catch (OperationCanceledException) { logger.LogWarning("Quartz shutdown reached its bounded deadline"); }
        catch (Exception exception) { logger.LogWarning(exception, "Quartz shutdown failed"); }
    }

    public void Dispose()
    {
        // App.Exit awaits ShutdownAsync while the XAML dispatcher is still alive.
        // The final root-container disposal must never block that dead dispatcher.
        ShutdownAsync().SafeForget();
    }

    private static async Task StopSchedulerAsync(IScheduler current)
    {
        await current.Shutdown(false, CancellationToken.None).ConfigureAwait(false);
    }
}
