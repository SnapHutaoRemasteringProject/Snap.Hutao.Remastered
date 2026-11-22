// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;

namespace Snap.Hutao.Remastered.Service.Game;

public static class GameLifeCycle
{
    private static readonly TimeSpan DiscoveryInterval = TimeSpan.FromSeconds(2);
    private static TaskCompletionSource refreshRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static IObservableProperty<bool> IsGameRunningProperty { get; } = Property.CreateObservable(PrivateIsGameRunning());

    public static IObservableProperty<bool> IsIslandConnected { get; } = Property.CreateObservable(false);

    public static async ValueTask SpinWaitGameExitAsync(ITaskContext taskContext)
    {
        while (true)
        {
            Task refresh = Volatile.Read(ref refreshRequested).Task;
            await taskContext.SwitchToBackgroundAsync();
            List<DiagnosticsProcess> processes = GetGameProcesses();
            if (processes.Count == 0)
            {
                await taskContext.SwitchToMainThreadAsync();
                if (refresh.IsCompleted)
                {
                    continue;
                }

                UpdateGameRunningState(false);
                return;
            }

            try
            {
                await WaitForGameProcessesAsync(processes, refresh, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                DisposeProcesses(processes);
            }
        }
    }

    public static async Task ObserveGameRunningAsync(ITaskContext taskContext, CancellationToken token)
    {
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                Task refresh = Volatile.Read(ref refreshRequested).Task;
                await taskContext.SwitchToBackgroundAsync();
                List<DiagnosticsProcess> processes = GetGameProcesses();
                try
                {
                    await taskContext.SwitchToMainThreadAsync();
                    token.ThrowIfCancellationRequested();
                    if (refresh.IsCompleted)
                    {
                        continue;
                    }

                    UpdateGameRunningState(processes.Count > 0);
                    await taskContext.SwitchToBackgroundAsync();
                    await WaitForGameProcessesAsync(processes, refresh, token).ConfigureAwait(false);
                }
                finally
                {
                    DisposeProcesses(processes);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The launch page was unloaded. Its observer must stop without changing shared game state.
        }
    }

    public static bool IsGameRunningRequiresMainThread()
    {
        bool result = PrivateIsGameRunning();
        UpdateGameRunningState(result);
        return result;
    }

    public static async ValueTask<bool> IsGameRunningAsync(ITaskContext taskContext)
    {
        await taskContext.SwitchToBackgroundAsync();
        bool result = PrivateIsGameRunning();

        await taskContext.SwitchToMainThreadAsync();
        UpdateGameRunningState(result);
        return result;
    }

    public static async ValueTask<ValueResult<bool, IProcess?>> TryGetRunningGameProcessAsync(ITaskContext taskContext)
    {
        await taskContext.SwitchToBackgroundAsync();
        bool result = PrivateIsGameRunning(out IProcess? runningProcess);

        await taskContext.SwitchToMainThreadAsync();
        UpdateGameRunningState(result);
        return new(result, runningProcess);
    }

    public static async ValueTask<bool> TryKillGameProcessAsync(ITaskContext taskContext)
    {
        await taskContext.SwitchToBackgroundAsync();
        List<DiagnosticsProcess> processes = GetGameProcesses();
        Dictionary<int, Exception> failures = [];
        HashSet<int> remainingIds = [];
        try
        {
            foreach (DiagnosticsProcess process in processes)
            {
                try
                {
                    using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
                    await process.KillAndWaitForExitAsync(timeout.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ex.Data["GameProcessId"] = process.Id;
                    SentrySdk.CaptureException(ex);
                    failures.TryAdd(process.Id, ex);
                }
            }

            List<DiagnosticsProcess> remaining = GetGameProcesses();
            try
            {
                remainingIds.UnionWith(remaining.Select(process => process.Id));
                // A handle we are already tracking can outlast a fresh enumeration snapshot.
                remainingIds.UnionWith(processes.Where(process => !process.HasExited).Select(process => process.Id));
            }
            finally
            {
                DisposeProcesses(remaining);
            }
        }
        finally
        {
            DisposeProcesses(processes);
        }

        await taskContext.SwitchToMainThreadAsync();
        bool isRunning = remainingIds.Count > 0;
        Interlocked.Exchange(ref refreshRequested, new(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();
        UpdateGameRunningState(isRunning);
        if (isRunning)
        {
            Exception? failure = failures.FirstOrDefault(entry => remainingIds.Contains(entry.Key)).Value;
            string message = failure switch
            {
                Win32Exception { NativeErrorCode: (int)WIN32_ERROR.ERROR_ACCESS_DENIED } => SH.ServiceGameStopProcessAccessDenied,
                null or OperationCanceledException => $"{SH.ServiceGameLaunchPhaseWaitingProcessExit} (PID {string.Join(", ", remainingIds)})",
                _ => failure.Message,
            };
            HutaoNative.Instance.ShowErrorMessage(SH.ServiceGameLaunchExecutionGameRunningKillFailed, message);
        }

        return !isRunning;
    }

    private static List<DiagnosticsProcess> GetGameProcesses()
    {
        return ProcessFactory.GetRunningProcesses([GameConstants.YuanShenProcessName, GameConstants.GenshinImpactProcessName]);
    }

    private static void DisposeProcesses(List<DiagnosticsProcess> processes)
    {
        foreach (DiagnosticsProcess process in processes)
        {
            process.Dispose();
        }
    }

    private static async Task WaitForGameProcessesAsync(List<DiagnosticsProcess> processes, Task refresh, CancellationToken token)
    {
        using CancellationTokenSource pending = CancellationTokenSource.CreateLinkedTokenSource(token);
        Task[] waits = processes.Count == 0
            ? [Task.Delay(DiscoveryInterval, pending.Token)]
            : processes.Select(process => process.WaitForExitAsync(pending.Token)).ToArray();

        // Rescan on any exit or after a termination attempt. Cancel the other waits
        // before releasing their process handles.
        await Task.WhenAny(waits.Append(refresh)).ConfigureAwait(false);
        pending.Cancel();
        try
        {
            await Task.WhenAll(waits).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            // Another process exited or the caller requested a fresh snapshot.
        }
        catch (InvalidOperationException)
        {
            // A process exited before the wait was registered. Refresh the snapshot.
        }
        catch (Win32Exception ex)
        {
            // Some elevated processes cannot be watched from a normal-integrity process.
            SentrySdk.CaptureException(ex);
            await Task.WhenAny(refresh, Task.Delay(DiscoveryInterval, token)).ConfigureAwait(false);
        }

        token.ThrowIfCancellationRequested();
    }

    private static void UpdateGameRunningState(bool isRunning)
    {
        if (!isRunning)
        {
            // The Island monitor can still be awaiting a network response after the process exits.
            IsIslandConnected.Value = false;
        }

        IsGameRunningProperty.Value = isRunning;
    }

    private static bool PrivateIsGameRunning()
    {
        bool running = PrivateIsGameRunning(out IProcess? process);
        process?.Dispose();
        return running;
    }

    private static bool PrivateIsGameRunning([NotNullWhen(true)] out IProcess? runningProcess)
    {
        return ProcessFactory.IsRunning([GameConstants.YuanShenProcessName, GameConstants.GenshinImpactProcessName], out runningProcess);
    }
}
