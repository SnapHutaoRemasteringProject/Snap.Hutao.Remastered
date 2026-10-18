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
            foreach (IJobScheduler jobScheduler in serviceProvider.GetServices<IJobScheduler>())
            {
                token.ThrowIfCancellationRequested();
                await jobScheduler.ScheduleAsync(current, token).ConfigureAwait(false);
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

    public async Task StopJobAsync(string group, string triggerName, CancellationToken token = default)
    {
        await startupCompleted.Task.WaitAsync(token).ConfigureAwait(false);
        if (scheduler is { } current && !lifetime.IsCancellationRequested)
            await current.UnscheduleJob(new(triggerName, group), token).ConfigureAwait(false);
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
