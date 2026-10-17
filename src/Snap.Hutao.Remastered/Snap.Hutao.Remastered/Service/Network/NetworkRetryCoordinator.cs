// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Web;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.Web.Request.Builder;
using System.Net.Http;
using Windows.Networking.Connectivity;

namespace Snap.Hutao.Remastered.Service.Network;

[Service(ServiceLifetime.Singleton, typeof(INetworkRetryCoordinator))]
public sealed partial class NetworkRetryCoordinator : INetworkRetryCoordinator, IDisposable
{
    private sealed class RetryJob(Func<CancellationToken, ValueTask<bool>> retryAsync)
    {
        public Func<CancellationToken, ValueTask<bool>> RetryAsync { get; set; } = retryAsync;

        public bool IsPending { get; set; }

        public long Revision { get; set; }

        public bool IsRunning { get; set; }
    }

    private readonly IServiceProvider serviceProvider;
    private readonly IMessenger messenger;
    private readonly ITaskContext taskContext;
    private readonly object syncRoot = new();
    private readonly Dictionary<string, RetryJob> jobs = new();
    private readonly CancellationTokenSource disposeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(XamlApplicationLifetime.ShutdownToken);
    private readonly CancellationToken lifetimeToken;

    private static readonly TimeSpan PeriodicProbeInterval = TimeSpan.FromSeconds(2);

    private bool? isInternetAvailable;
    private int hasShownOfflineWarning;
    private int evaluationVersion;
    private int isProcessingEvaluation;
    private int disposed;

    private bool IsStopping => Volatile.Read(ref disposed) is not 0 || XamlApplicationLifetime.Exiting;

    public NetworkRetryCoordinator(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        lifetimeToken = disposeCancellationTokenSource.Token;
        messenger = serviceProvider.GetRequiredService<IMessenger>();
        taskContext = serviceProvider.GetRequiredService<ITaskContext>();

        isInternetAvailable = HasInternetAccess();
        NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;

        StartPeriodicProbeAsync().SafeForget();
    }

    public IDisposable Register(string key, Func<CancellationToken, ValueTask<bool>> retryAsync)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(retryAsync);

        RetryJob job = new(retryAsync);
        lock (syncRoot)
        {
            ObjectDisposedException.ThrowIf(IsStopping, this);
            jobs[key] = job;
        }

        return new Registration(this, key, job);
    }

    public void MarkPending(string key, string warningMessage)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(warningMessage);
        if (IsStopping) return;

        bool shouldWarn = false;
        lock (syncRoot)
        {
            if (!jobs.TryGetValue(key, out RetryJob? job))
            {
                return;
            }

            shouldWarn = !job.IsPending;
            job.IsPending = true;
            job.Revision++;
        }

        if (shouldWarn && !string.IsNullOrEmpty(warningMessage))
        {
            MaybeNotifyOfflineWarning(warningMessage);
        }

        // The first failure is eligible immediately; repeated failures from the
        // retry itself must not recursively enqueue another request.
        if (shouldWarn) ScheduleEvaluationAsync(notifyRecovered: false).SafeForget();
    }

    public void ClearPending(string key)
    {
        lock (syncRoot)
        {
            if (jobs.TryGetValue(key, out RetryJob? job))
            {
                job.IsPending = false;
                job.Revision++;
            }
        }
    }

    public async ValueTask<bool> HasInternetAccessAsync(CancellationToken token = default)
    {
        if (IsStopping || token.IsCancellationRequested) return false;
        // NCSI reports InternetAccess → trust it, no HTTP round trip needed.
        if (HasInternetAccess())
        {
            return true;
        }

        // NCSI is not reliable: when it reports no Internet (e.g. probe hijacked and misjudged
        // as captive portal, see issue #268) it may stay stale and never fire NetworkStatusChanged.
        // Fall back to an active probe against the Snap Hutao API so a working network is not
        // mistaken for an offline one.
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token, lifetimeToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

        try
        {
            if (IsStopping) return false;
            using IServiceScope scope = serviceProvider.CreateScope();
            HutaoInfrastructureClient infrastructureClient = scope.ServiceProvider.GetRequiredService<HutaoInfrastructureClient>();
            return await infrastructureClient.IsReachableAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (ObjectDisposedException) when (IsStopping)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) is not 0) return;
        NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
        lock (syncRoot) jobs.Clear();
        disposeCancellationTokenSource.Cancel();
        disposeCancellationTokenSource.Dispose();
    }

    private void OnNetworkStatusChanged(object? sender)
    {
        if (IsStopping) return;
        ScheduleEvaluationAsync(true).SafeForget();
    }

    private async ValueTask StartPeriodicProbeAsync()
    {
        // Backstop for both stale NCSI reports (#268) and request failures on an
        // otherwise connected network. This does not delay an initial/manual retry.
        using PeriodicTimer timer = new(PeriodicProbeInterval);
        try
        {
            while (!IsStopping && await timer.WaitForNextTickAsync(lifetimeToken).ConfigureAwait(false))
            {
                if (IsStopping || !HasPendingJob())
                {
                    continue;
                }

                ScheduleEvaluationAsync(notifyRecovered: false).SafeForget();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private bool HasPendingJob()
    {
        lock (syncRoot)
        {
            return jobs.Values.Any(static job => job.IsPending);
        }
    }

    private async ValueTask ScheduleEvaluationAsync(bool notifyRecovered)
    {
        if (IsStopping) return;
        int version = Interlocked.Increment(ref evaluationVersion);

        try
        {
            await Task.Delay(500, lifetimeToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (IsStopping || version != Volatile.Read(ref evaluationVersion))
        {
            return;
        }

        Evaluate(notifyRecovered);
    }

    private void Evaluate(bool notifyRecovered)
    {
        if (IsStopping) return;
        if (Interlocked.CompareExchange(ref isProcessingEvaluation, 1, 0) is not 0)
        {
            return;
        }

        try
        {
            // Pending operations test their own endpoint. An unrelated /ip probe
            // or stale NCSI result must not veto repository-server recovery.
            bool internetAvailable = HasInternetAccess();
            if (IsStopping) return;
            bool? previousInternetAvailable = isInternetAvailable;
            isInternetAvailable = internetAvailable;

            if (previousInternetAvailable is not null && previousInternetAvailable != internetAvailable)
            {
                if (!internetAvailable)
                {
                    Volatile.Write(ref hasShownOfflineWarning, 0);
                    NotifyOnMainThread(InfoBarMessage.Warning(SH.ViewModelMainNetworkDisconnected));
                    Volatile.Write(ref hasShownOfflineWarning, 1);
                    return;
                }

                Volatile.Write(ref hasShownOfflineWarning, 0);
            }

            List<(string Key, RetryJob Job)> pendingJobs;
            lock (syncRoot)
            {
                pendingJobs = jobs
                    .Where(static pair => pair.Value.IsPending && !pair.Value.IsRunning)
                    .Select(static pair => (pair.Key, pair.Value))
                    .ToList();
            }

            if (pendingJobs.Count is 0)
            {
                return;
            }

            if (notifyRecovered)
            {
                NotifyOnMainThread(InfoBarMessage.Information(SH.ViewModelMainNetworkRecoveredRetrying));
            }

            foreach ((string key, RetryJob job) in pendingJobs)
            {
                if (IsStopping) break;
                long revision;
                lock (syncRoot)
                {
                    if (!jobs.TryGetValue(key, out RetryJob? registered) || !ReferenceEquals(registered, job) || !job.IsPending || job.IsRunning) continue;
                    job.IsRunning = true;
                    revision = job.Revision;
                }
                RetryJobAsync(key, job, revision).SafeForget();
            }
        }
        finally
        {
            Interlocked.Exchange(ref isProcessingEvaluation, 0);
        }
    }

    private async Task RetryJobAsync(string key, RetryJob job, long revision)
    {
        // A user/card retry may wait for metadata. It must not hold up the
        // independent startup retry that produces that metadata.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        bool succeeded = false;
        try
        {
            if (!IsStopping) succeeded = await job.RetryAsync(lifetimeToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) when (IsStopping) { }
        catch (Exception exception) when (IsNetworkRelatedException(exception)) { }
        catch (Exception exception) { SentrySdk.CaptureException(exception); }
        finally
        {
            lock (syncRoot)
            {
                if (jobs.TryGetValue(key, out RetryJob? current) && ReferenceEquals(current, job))
                {
                    // Do not erase a new failure reported during this attempt.
                    if (current.Revision == revision) current.IsPending = !succeeded;
                    current.IsRunning = false;
                }
            }
        }
    }

    private static bool HasInternetAccess()
    {
        // This approach is not 100% accurate, but it's good enough for our scenario.
        // https://learn.microsoft.com/uwp/api/windows.networking.connectivity.networkconnectivitylevel
        return NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel() is NetworkConnectivityLevel.InternetAccess;
    }

    private static bool IsNetworkRelatedException(Exception ex)
    {
        return ex switch
        {
            HttpRequestException httpRequestException => HttpRequestExceptionHandling.HttpRequestExceptionToNetworkError(httpRequestException) is not NetworkError.NULL,
            TimeoutException => true,
            OperationCanceledException => true,
            _ => false,
        };
    }

    private void NotifyOnMainThread(InfoBarMessage message)
    {
        if (IsStopping) return;
        taskContext.BeginInvokeOnMainThread(() =>
        {
            if (!IsStopping) messenger.Send(message);
        });
    }

    private void MaybeNotifyOfflineWarning(string warningMessage)
    {
        if (HasInternetAccess())
        {
            NotifyOnMainThread(InfoBarMessage.Warning(warningMessage));
            return;
        }

        if (Interlocked.CompareExchange(ref hasShownOfflineWarning, 1, 0) is 0)
        {
            NotifyOnMainThread(InfoBarMessage.Warning(warningMessage));
        }
    }

    private sealed class Registration : IDisposable
    {
        private readonly NetworkRetryCoordinator owner;
        private readonly string key;
        private readonly RetryJob job;
        private bool disposed;

        public Registration(NetworkRetryCoordinator owner, string key, RetryJob job)
        {
            this.owner = owner;
            this.key = key;
            this.job = job;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            lock (owner.syncRoot)
            {
                if (owner.jobs.TryGetValue(key, out RetryJob? current) && ReferenceEquals(current, job)) owner.jobs.Remove(key);
            }
        }
    }
}
