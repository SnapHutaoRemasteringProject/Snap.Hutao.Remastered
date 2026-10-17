using Snap.Hutao.Remastered.Service.Git;
using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.Immutable;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class RepositoryCatalogRequestTest
{
    private static ImmutableArray<GitRepository> Live =>
    [
        new() { Name = "live", WebUrl = new("https://example.test/"), HttpsUrl = new("https://example.test/repo.git"), Type = GitRepositoryType.Public },
    ];

    [TestMethod]
    public async Task SuccessUsesLiveDataWithoutRetry()
    {
        RepositoryCatalogRequest policy = new();
        int requests = 0;
        ImmutableArray<GitRepository> result = await policy.FetchAsync(_ => { requests++; return Task.FromResult(Live); }, default);
        Assert.AreEqual(1, requests);
        Assert.AreEqual("live", result[0].Name);
    }

    [TestMethod]
    public async Task TransportFailureMustOccurTwiceBeforeFallbackIsAllowed()
    {
        RepositoryCatalogRequest policy = new();
        int requests = 0;
        List<int> attempts = [];
        ImmutableArray<GitRepository> result = await policy.FetchAsync(_ =>
        {
            requests++;
            return Task.FromException<ImmutableArray<GitRepository>>(new HttpRequestException("offline"));
        }, default, (attempt, _) => attempts.Add(attempt));
        Assert.IsTrue(result.IsDefault);
        Assert.AreEqual(2, requests);
        CollectionAssert.AreEqual(new[] { 1, 2 }, attempts);
    }

    [TestMethod]
    public async Task ARecoveredSecondAttemptDoesNotSelectFallback()
    {
        RepositoryCatalogRequest policy = new();
        int requests = 0;
        ImmutableArray<GitRepository> result = await policy.FetchAsync(_ => ++requests == 1
            ? Task.FromException<ImmutableArray<GitRepository>>(new HttpRequestException("temporary")) : Task.FromResult(Live), default);
        Assert.AreEqual(2, requests);
        Assert.AreEqual("live", result[0].Name);
    }

    [TestMethod]
    public async Task BusinessAndJsonErrorsDoNotMasqueradeAsOffline()
    {
        foreach (Exception failure in new Exception[] { new InvalidOperationException("business failure"), new JsonException("bad json") })
        {
            RepositoryCatalogRequest policy = new();
            int requests = 0;
            try
            {
                await policy.FetchAsync(_ => { requests++; return Task.FromException<ImmutableArray<GitRepository>>(failure); }, default);
                Assert.Fail("An online response error must not select fallback");
            }
            catch (Exception exception) when (ReferenceEquals(exception, failure)) { }
            Assert.AreEqual(1, requests);
        }
    }

    [TestMethod]
    public async Task ApplicationCancellationDoesNotSelectFallback()
    {
        using CancellationTokenSource cancellation = new();
        RepositoryCatalogRequest policy = new();
        int requests = 0;
        try
        {
            await policy.FetchAsync(token =>
            {
                requests++;
                cancellation.Cancel();
                return Task.FromCanceled<ImmutableArray<GitRepository>>(token);
            }, cancellation.Token);
            Assert.Fail("Application cancellation must propagate");
        }
        catch (OperationCanceledException) { }
        Assert.AreEqual(1, requests);
    }

    [TestMethod]
    public async Task FailedRequestRetriesWithoutAnAddedDelay()
    {
        RepositoryCatalogRequest policy = new();
        TaskCompletionSource secondRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int requests = 0;
        Task<ImmutableArray<GitRepository>> result = policy.FetchAsync(_ =>
        {
            if (++requests == 1) return Task.FromException<ImmutableArray<GitRepository>>(new HttpRequestException("temporary"));
            secondRequest.SetResult();
            return Task.FromResult(Live);
        }, default);
        await secondRequest.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.AreEqual("live", (await result)[0].Name);
    }

    [TestMethod]
    public async Task DefaultDeadlineAllowsMoreThanThreeSecondsButCancelsAtEight()
    {
        RepositoryCatalogRequest policy = new();
        using CancellationTokenSource caller = new();
        System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();
        int requests = 0;
        try
        {
            await policy.FetchAsync(async token =>
            {
                requests++;
                await Task.Delay(3500, token);
                // The previous three-second deadline would cancel before this.
                Assert.IsFalse(token.IsCancellationRequested);
                try { await Task.Delay(Timeout.Infinite, token); }
                finally { caller.Cancel(); }
                return Live;
            }, caller.Token);
            Assert.Fail("Expected the eight-second deadline to cancel the request");
        }
        catch (OperationCanceledException) { }
        Assert.AreEqual(1, requests);
        Assert.IsTrue(elapsed.Elapsed >= TimeSpan.FromSeconds(7.5));
        Assert.IsTrue(elapsed.Elapsed < TimeSpan.FromSeconds(12));
    }

    [TestMethod]
    public async Task DeadlineCancelsEachAttemptBeforeRetrying()
    {
        RepositoryCatalogRequest policy = new(TimeSpan.FromMilliseconds(15));
        int requests = 0;
        int canceled = 0;
        ImmutableArray<GitRepository> result = await policy.FetchAsync(async token =>
        {
            Interlocked.Increment(ref requests);
            try { await Task.Delay(Timeout.Infinite, token); }
            catch (OperationCanceledException) { Interlocked.Increment(ref canceled); throw; }
            return Live;
        }, default);
        Assert.IsTrue(result.IsDefault);
        Assert.AreEqual(2, requests);
        Assert.AreEqual(2, canceled);
    }
}
