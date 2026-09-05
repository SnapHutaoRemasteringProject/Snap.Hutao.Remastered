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

        public string? WarningMessage { get; set; }
    }

    private readonly IServiceProvider serviceProvider;
    private readonly IMessenger messenger;
    private readonly ITaskContext taskContext;
    private readonly object syncRoot = new();
    private readonly Dictionary<string, RetryJob> jobs = new();
    private readonly CancellationTokenSource disposeCancellationTokenSource = new();

    private static readonly TimeSpan PeriodicProbeInterval = TimeSpan.FromSeconds(15);

    private bool? isInternetAvailable;
    private int hasShownOfflineWarning;
    private int evaluationVersion;
    private int isProcessingEvaluation;

    public NetworkRetryCoordinator(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
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

        lock (syncRoot)
        {
            jobs[key] = new(retryAsync);
        }

        return new Registration(this, key);
    }

    public void MarkPending(string key, string warningMessage)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(warningMessage);

        bool shouldWarn = false;
        lock (syncRoot)
        {
            if (!jobs.TryGetValue(key, out RetryJob? job))
            {
                return;
            }

            shouldWarn = !job.IsPending;
            job.IsPending = true;
            job.WarningMessage = warningMessage;
        }

        if (shouldWarn)
        {
            MaybeNotifyOfflineWarning(warningMessage);
        }

        if (HasInternetAccess())
        {
            ScheduleEvaluationAsync(false).SafeForget();
        }
    }

    public void ClearPending(string key)
    {
        lock (syncRoot)
        {
            if (jobs.TryGetValue(key, out RetryJob? job))
            {
                job.IsPending = false;
            }
        }
    }

    public async ValueTask<bool> HasInternetAccessAsync(CancellationToken token = default)
    {
        // NCSI reports InternetAccess → trust it, no HTTP round trip needed.
        if (HasInternetAccess())
        {
            return true;
        }

        // NCSI is not reliable: when it reports no Internet (e.g. probe hijacked and misjudged
        // as captive portal, see issue #268) it may stay stale and never fire NetworkStatusChanged.
        // Fall back to an active probe against the Snap Hutao API so a working network is not
        // mistaken for an offline one.
        using IServiceScope scope = serviceProvider.CreateScope();
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

        try
        {
            HutaoInfrastructureClient infrastructureClient = scope.ServiceProvider.GetRequiredService<HutaoInfrastructureClient>();
            return await infrastructureClient.IsReachableAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        disposeCancellationTokenSource.Cancel();
        NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
        disposeCancellationTokenSource.Dispose();
    }

    private void OnNetworkStatusChanged(object? sender)
    {
        ScheduleEvaluationAsync(true).SafeForget();
    }

    private async ValueTask StartPeriodicProbeAsync()
    {
        // Periodic backstop: when a job is pending and NCSI keeps reporting "no Internet",
        // Windows may never raise NetworkStatusChanged again (e.g. captive-portal false positive,
        // issue #268). Re-check periodically so recovery is eventually detected.
        using PeriodicTimer timer = new(PeriodicProbeInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(disposeCancellationTokenSource.Token).ConfigureAwait(false))
            {
                // If NCSI now reports Internet, rely on the regular event-driven path.
                if (!HasPendingJob() || HasInternetAccess())
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
        int version = Interlocked.Increment(ref evaluationVersion);

        try
        {
            await Task.Delay(500, disposeCancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (version != Volatile.Read(ref evaluationVersion))
        {
            return;
        }

        await EvaluateAsync(notifyRecovered).ConfigureAwait(false);
    }

    private async ValueTask EvaluateAsync(bool notifyRecovered)
    {
        if (Interlocked.CompareExchange(ref isProcessingEvaluation, 1, 0) is not 0)
        {
            return;
        }

        try
        {
            bool internetAvailable = await HasInternetAccessAsync(disposeCancellationTokenSource.Token).ConfigureAwait(false);
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
                    .Where(static pair => pair.Value.IsPending)
                    .Select(static pair => (pair.Key, pair.Value))
                    .ToList();
            }

            if (pendingJobs.Count is 0)
            {
                return;
            }

            if (!internetAvailable)
            {
                return;
            }

            if (notifyRecovered)
            {
                NotifyOnMainThread(InfoBarMessage.Information(SH.ViewModelMainNetworkRecoveredRetrying));
            }

            foreach ((string key, RetryJob job) in pendingJobs)
            {
                bool succeeded;
                try
                {
                    succeeded = await job.RetryAsync(disposeCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (IsNetworkRelatedException(ex))
                {
                    succeeded = false;
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    succeeded = false;
                }

                lock (syncRoot)
                {
                    if (jobs.TryGetValue(key, out RetryJob? currentJob))
                    {
                        currentJob.IsPending = !succeeded;
                    }
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref isProcessingEvaluation, 0);
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
            TaskCanceledException => true,
            _ => false,
        };
    }

    private void NotifyOnMainThread(InfoBarMessage message)
    {
        taskContext.BeginInvokeOnMainThread(() => messenger.Send(message));
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
        private bool disposed;

        public Registration(NetworkRetryCoordinator owner, string key)
        {
            this.owner = owner;
            this.key = key;
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
                owner.jobs.Remove(key);
            }
        }
    }
}
