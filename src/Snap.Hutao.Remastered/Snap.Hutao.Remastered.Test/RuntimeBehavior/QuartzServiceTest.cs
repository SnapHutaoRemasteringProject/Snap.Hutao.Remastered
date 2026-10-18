using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Snap.Hutao.Remastered.Service.Job;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class QuartzServiceTest
{
    [TestMethod]
    public async Task StartupDoesNotWaitForAccountDependentJobRegistration()
    {
        BlockingRegistration registration = new();
        using ServiceProvider provider = CreateProvider(registration);
        using QuartzService service = new(provider);
        await service.StartAsync().WaitAsync(TimeSpan.FromSeconds(3));
        await registration.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.IsFalse(registration.Stopped.Task.IsCompleted);
        IScheduler scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        await service.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.IsTrue(registration.Stopped.Task.IsCompleted);
        Assert.AreEqual(SchedulerStatus.Shutdown, scheduler.Status);
    }

    [TestMethod]
    public async Task ShutdownCancelsAndJoinsRegistrationBeforeTheProviderIsDisposed()
    {
        BlockingRegistration registration = new();
        using ServiceProvider provider = CreateProvider(registration);
        using QuartzService service = new(provider);
        await service.StartAsync();
        await registration.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await service.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.IsTrue(registration.CancellationObserved);
        Assert.IsTrue(registration.Stopped.Task.IsCompleted);
    }

    [TestMethod]
    public async Task RepeatedStartAndShutdownShareTheirOperations()
    {
        using ServiceProvider provider = CreateProvider(new BlockingRegistration());
        using QuartzService service = new(provider);
        Task starting = service.StartAsync();
        Assert.AreSame(starting, service.StartAsync());
        await starting;
        Task stopping = service.ShutdownAsync();
        Assert.AreSame(stopping, service.ShutdownAsync());
        await stopping.WaitAsync(TimeSpan.FromSeconds(3));
    }

    [TestMethod]
    public async Task ARegistrationFailureDoesNotPreventSchedulerShutdown()
    {
        using ServiceProvider provider = CreateProvider(new FailedRegistration());
        using QuartzService service = new(provider);
        await service.StartAsync();
        IScheduler scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler();
        await service.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.AreEqual(SchedulerStatus.Shutdown, scheduler.Status);
    }

    private static ServiceProvider CreateProvider(IJobScheduler registration)
    {
        return new ServiceCollection()
            .AddQuartz()
            .AddSingleton<ILogger<QuartzService>>(NullLogger<QuartzService>.Instance)
            .AddSingleton(registration)
            .BuildServiceProvider();
    }

    private sealed class BlockingRegistration : IJobScheduler
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool CancellationObserved { get; private set; }

        public async ValueTask ScheduleAsync(IScheduler scheduler, CancellationToken token = default)
        {
            Started.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            finally
            {
                CancellationObserved = token.IsCancellationRequested;
                Stopped.TrySetResult();
            }
        }
    }

    private sealed class FailedRegistration : IJobScheduler
    {
        public ValueTask ScheduleAsync(IScheduler scheduler, CancellationToken token = default) => ValueTask.FromException(new InvalidOperationException("Registration failed"));
    }
}
