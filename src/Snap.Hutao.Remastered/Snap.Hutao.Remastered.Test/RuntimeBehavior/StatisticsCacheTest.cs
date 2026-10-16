using Snap.Hutao.Remastered.Service.Hutao;
using System.Linq;
using System.Threading;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class StatisticsCacheTest
{
    [TestMethod]
    public async Task ConcurrentCallersWaitForOneInitialization()
    {
        TestCache cache = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        Task[] loads = Enumerable.Range(0, 16).Select(_ => cache.LoadAsync<FirstKey>(() =>
        {
            Interlocked.Increment(ref calls);
            return release.Task;
        })).ToArray();

        Assert.AreEqual(1, calls);
        Assert.IsTrue(loads.All(static load => !load.IsCompleted));
        release.SetResult();
        await Task.WhenAll(loads).WaitAsync(TimeSpan.FromSeconds(5));
        await cache.LoadAsync<FirstKey>(() => throw new InvalidOperationException("Successful initialization must stay cached."));
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public async Task FailureCompletesAllWaitersAndAllowsRetry()
    {
        TestCache cache = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task[] loads = Enumerable.Range(0, 8).Select(_ => cache.LoadAsync<FirstKey>(() => release.Task)).ToArray();
        InvalidOperationException expected = new("Network initialization failed.");
        release.SetException(expected);

        foreach (Task load in loads)
        {
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => load.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.AreSame(expected, actual);
        }

        bool retried = false;
        await cache.LoadAsync<FirstKey>(() =>
        {
            retried = true;
            return Task.CompletedTask;
        }).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(retried);
    }

    [TestMethod]
    public async Task DifferentPageKeysInitializeIndependently()
    {
        TestCache cache = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task blocked = cache.LoadAsync<FirstKey>(() => release.Task);
        await cache.LoadAsync<SecondKey>(() => Task.CompletedTask).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsFalse(blocked.IsCompleted);
        release.SetResult();
        await blocked.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class FirstKey;
    private sealed class SecondKey;

    private sealed class TestCache : StatisticsCache
    {
        public Task LoadAsync<TKey>(Func<Task> initialize) =>
            InitializeForTypeAsync<TKey, Func<Task>>(initialize, static action => action()).AsTask();
    }
}
