using Snap.Hutao.Remastered.Core.Threading;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class AsyncLoadOperationTest
{
    [TestMethod]
    public async Task ConcurrentAndLaterLoadsReuseOneSuccessfulRequest()
    {
        AsyncLoadOperation operation = new();
        TaskCompletionSource<bool> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int requests = 0;
        Task<bool> Load(CancellationToken _) { Interlocked.Increment(ref requests); return response.Task; }
        Task<bool>[] callers = Enumerable.Range(0, 20).Select(_ => operation.RunAsync(Load, default)).ToArray();
        Assert.AreEqual(1, requests);
        response.SetResult(true);
        Assert.IsTrue((await Task.WhenAll(callers)).All(result => result));
        Assert.IsTrue(await operation.RunAsync(Load, default));
        Assert.AreEqual(1, requests);
    }

    [TestMethod]
    public async Task RetryingFailedPartDoesNotReloadSuccessfulParts()
    {
        AsyncLoadOperation success = new();
        AsyncLoadOperation failure = new();
        int successfulRequests = 0;
        int failedRequests = 0;
        Task<bool> LoadSuccess(CancellationToken _) { successfulRequests++; return Task.FromResult(true); }
        Task<bool> LoadFailure(CancellationToken _) { failedRequests++; return Task.FromResult(false); }
        for (int attempt = 0; attempt < 4; attempt++)
            await Task.WhenAll(success.RunAsync(LoadSuccess, default), failure.RunAsync(LoadFailure, default));
        Assert.AreEqual(1, successfulRequests);
        Assert.AreEqual(4, failedRequests);
    }

    [TestMethod]
    public async Task FaultReleasesEveryWaiterAndAllowsALaterRetry()
    {
        AsyncLoadOperation operation = new();
        TaskCompletionSource<bool> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool>[] callers = Enumerable.Range(0, 4).Select(_ => operation.RunAsync(_ => response.Task, default)).ToArray();
        response.SetException(new InvalidOperationException("test"));
        foreach (Task<bool> caller in callers)
        {
            try { await caller.WaitAsync(TimeSpan.FromSeconds(2)); Assert.Fail("Expected failure"); }
            catch (InvalidOperationException exception) { Assert.AreEqual("test", exception.Message); }
        }
        Assert.IsTrue(await operation.RunAsync(_ => Task.FromResult(true), default));
    }

    [TestMethod]
    public async Task CancelingAWaiterDoesNotCancelTheSharedRequest()
    {
        AsyncLoadOperation operation = new();
        TaskCompletionSource<bool> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool> owner = operation.RunAsync(_ => response.Task, default);
        using CancellationTokenSource waiterCancellation = new();
        Task<bool> waiter = operation.RunAsync(_ => throw new InvalidOperationException("duplicate"), waiterCancellation.Token);
        waiterCancellation.Cancel();
        try { await waiter; Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException) { }
        Assert.IsFalse(owner.IsCompleted);
        response.SetResult(true);
        Assert.IsTrue(await owner);
    }

    [TestMethod]
    public async Task CanceledLoadDoesNotPoisonTheNextAttempt()
    {
        AsyncLoadOperation operation = new();
        using CancellationTokenSource cancellation = new();
        Task<bool> pending = operation.RunAsync(async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return true;
        }, cancellation.Token);
        cancellation.Cancel();
        try { await pending; Assert.Fail("Expected cancellation"); }
        catch (OperationCanceledException) { }
        Assert.IsTrue(await operation.RunAsync(_ => Task.FromResult(true), default));
    }

}
