using Snap.Hutao.Remastered.Core.Threading;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class AsyncCoordinationTest
{
    [TestMethod]
    public async Task SemaphoreDoesNotRunWaiterOnReleasingThread()
    {
        AsyncSemaphore semaphore = new(0, 1);
        Task wait = semaphore.WaitAsync();
        int continuationThread = 0;
        int releasingThread = 0;
        Task continuation = wait.ContinueWith(_ => continuationThread = Environment.CurrentManagedThreadId,
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        Thread release = new(() => { releasingThread = Environment.CurrentManagedThreadId; semaphore.Release(); });
        release.Start();
        Assert.IsTrue(release.Join(TimeSpan.FromSeconds(5)));
        await continuation.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreNotEqual(releasingThread, continuationThread);
    }

    [TestMethod]
    public async Task SlowWaiterDoesNotHoldGlobalKeyedLock()
    {
        AsyncKeyedLock<string> keyed = new();
        AsyncKeyedLock<string>.Releaser first = await keyed.LockAsync("first");
        Task<AsyncKeyedLock<string>.Releaser> waiter = keyed.LockAsync("first");
        using ManualResetEventSlim started = new();
        using ManualResetEventSlim unblock = new();
        Task slow = waiter.ContinueWith(task =>
        {
            using (task.Result)
            {
                started.Set();
                if (!unblock.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException(); }
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        Thread release = new(first.Dispose);
        release.Start();
        try
        {
            Assert.IsTrue(started.Wait(TimeSpan.FromSeconds(5)));
            Task independent = Task.Run(async () => { using (await keyed.LockAsync("second")) { } });
            await independent.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(release.Join(TimeSpan.FromSeconds(5)));
        }
        finally { unblock.Set(); }
        await slow.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task SameKeyRemainsExclusiveUnderContention()
    {
        AsyncKeyedLock<string> keyed = new();
        int inside = 0;
        int maximum = 0;
        Task[] tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(async () =>
        {
            for (int i = 0; i < 10; i++)
            {
                using (await keyed.LockAsync("same"))
                {
                    int count = Interlocked.Increment(ref inside);
                    Interlocked.Exchange(ref maximum, Math.Max(maximum, count));
                    await Task.Yield();
                    Interlocked.Decrement(ref inside);
                }
            }
        })).ToArray();
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual(1, maximum);
    }
}
