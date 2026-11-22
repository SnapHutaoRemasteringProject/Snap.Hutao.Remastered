using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Service.Game;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
[DoNotParallelize]
public sealed class GameLifeCycleConnectionTest
{
    [TestInitialize]
    public void Initialize()
    {
        ProcessFactory.Current = null;
        ProcessFactory.Additional.Clear();
        GameLifeCycle.IsGameRunningRequiresMainThread();
        ProcessFactory.DiscoveryCount = 0;
        ProcessFactory.Handles.Clear();
        Win32.HutaoNative.Errors.Clear();
    }

    [TestCleanup]
    public void Cleanup()
    {
        ProcessFactory.Current = null;
        ProcessFactory.Additional.Clear();
        GameLifeCycle.IsGameRunningRequiresMainThread();
        ProcessFactory.Handles.Clear();
        Win32.HutaoNative.Errors.Clear();
    }

    [TestMethod]
    [DataRow("SynchronousProbe")]
    [DataRow("AsynchronousProbe")]
    [DataRow("ProcessLookup")]
    [DataRow("ExitWait")]
    [DataRow("Kill")]
    public async Task ObservedExitClearsConnectionBeforeIslandMonitorFinishes(string path)
    {
        ProcessFactory.Current = path == "Kill" ? new FakeProcess() : null;
        GameLifeCycle.IsGameRunningProperty.Value = true;
        GameLifeCycle.IsIslandConnected.Value = true;
        List<bool> connectionChanges = [];
        PropertyChangedEventHandler changed = (_, _) => connectionChanges.Add(GameLifeCycle.IsIslandConnected.Value);
        GameLifeCycle.IsIslandConnected.PropertyChanged += changed;
        try
        {
            TaskContext context = new();
            switch (path)
            {
                case "SynchronousProbe": Assert.IsFalse(GameLifeCycle.IsGameRunningRequiresMainThread()); break;
                case "AsynchronousProbe": Assert.IsFalse(await GameLifeCycle.IsGameRunningAsync(context)); break;
                case "ProcessLookup": await GameLifeCycle.TryGetRunningGameProcessAsync(context); break;
                case "ExitWait": await GameLifeCycle.SpinWaitGameExitAsync(context); break;
                case "Kill": Assert.IsTrue(await GameLifeCycle.TryKillGameProcessAsync(context)); break;
            }

            Assert.IsFalse(GameLifeCycle.IsIslandConnected.Value);
            Assert.IsFalse(GameLifeCycle.IsGameRunningProperty.Value);
            CollectionAssert.AreEqual(new[] { false }, connectionChanges);
        }
        finally
        {
            GameLifeCycle.IsIslandConnected.PropertyChanged -= changed;
        }
    }

    [TestMethod]
    public void ProbeClearsAStaleConnectionEvenWhenRunningStateIsAlreadyFalse()
    {
        ProcessFactory.Current = null;
        GameLifeCycle.IsGameRunningProperty.Value = false;
        GameLifeCycle.IsIslandConnected.Value = true;
        Assert.IsFalse(GameLifeCycle.IsGameRunningRequiresMainThread());
        Assert.IsFalse(GameLifeCycle.IsIslandConnected.Value);
    }

    [TestMethod]
    public async Task RunningGamePreservesItsConnectionAndDoesNotReconnectAfterAProbe()
    {
        ProcessFactory.Current = new FakeProcess();
        GameLifeCycle.IsIslandConnected.Value = true;
        Assert.IsTrue(await GameLifeCycle.IsGameRunningAsync(new TaskContext()));
        Assert.IsTrue(GameLifeCycle.IsIslandConnected.Value);
        GameLifeCycle.IsIslandConnected.Value = false;
        Assert.IsTrue(GameLifeCycle.IsGameRunningRequiresMainThread());
        Assert.IsFalse(GameLifeCycle.IsIslandConnected.Value);
    }

    [TestMethod]
    public async Task ExternalGameIsObservedWithoutPollingWhileItRunsAndItsExitClearsTheState()
    {
        FakeProcess process = new();
        ProcessFactory.Current = process;
        using CancellationTokenSource stop = new();
        Task observing = GameLifeCycle.ObserveGameRunningAsync(new TaskContext(), stop.Token);
        try
        {
            await WaitUntilAsync(static () => GameLifeCycle.IsGameRunningProperty.Value);
            int discoveries = ProcessFactory.DiscoveryCount;
            await Task.Delay(150);
            Assert.AreEqual(discoveries, ProcessFactory.DiscoveryCount, "A running game should use its exit notification.");
            process.Exit();
            await WaitUntilAsync(static () => !GameLifeCycle.IsGameRunningProperty.Value);
        }
        finally
        {
            stop.Cancel();
            await observing.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.IsTrue(ProcessFactory.Handles.All(static handle => handle.IsDisposed));
    }

    [TestMethod]
    public async Task AGameStartedAfterOpeningThePageIsDiscoveredAndUnloadingDoesNotClearItsState()
    {
        using CancellationTokenSource stop = new();
        Task observing = GameLifeCycle.ObserveGameRunningAsync(new TaskContext(), stop.Token);
        try
        {
            await WaitUntilAsync(static () => ProcessFactory.DiscoveryCount > 0);
            ProcessFactory.Current = new FakeProcess();
            await WaitUntilAsync(static () => GameLifeCycle.IsGameRunningProperty.Value);
        }
        finally
        {
            stop.Cancel();
            await observing.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.IsTrue(GameLifeCycle.IsGameRunningProperty.Value);
        Assert.IsTrue(ProcessFactory.Handles.All(static handle => handle.IsDisposed));
    }

    [TestMethod]
    public async Task EndingTheGameTerminatesEveryMatchingProcessAndReleasesTheSnapshots()
    {
        FakeProcess first = new();
        FakeProcess second = new();
        ProcessFactory.Current = first;
        ProcessFactory.Additional.Add(second);
        Assert.IsTrue(await GameLifeCycle.TryKillGameProcessAsync(new TaskContext()));
        Assert.AreEqual(1, first.KillCount);
        Assert.AreEqual(1, second.KillCount);
        Assert.IsFalse(GameLifeCycle.IsGameRunningProperty.Value);
        Assert.IsTrue(ProcessFactory.Handles.All(static handle => handle.IsDisposed));
        Assert.IsEmpty(Win32.HutaoNative.Errors);
    }

    [TestMethod]
    public async Task EndingTheGameWaitsForTheActualExit()
    {
        FakeProcess process = new() { ExitOnKill = false };
        ProcessFactory.Current = process;
        Task<bool> ending = GameLifeCycle.TryKillGameProcessAsync(new TaskContext()).AsTask();
        await WaitUntilAsync(() => process.KillCount > 0);
        Assert.IsFalse(ending.IsCompleted);
        process.Exit();
        Assert.IsTrue(await ending.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [TestMethod]
    public async Task PermissionFailureKeepsTheRunningStateAndDoesNotSkipOtherProcesses()
    {
        FakeProcess denied = new() { KillError = new Win32Exception(5) };
        FakeProcess allowed = new();
        ProcessFactory.Current = denied;
        ProcessFactory.Additional.Add(allowed);
        Assert.IsFalse(await GameLifeCycle.TryKillGameProcessAsync(new TaskContext()));
        Assert.AreEqual(1, denied.KillCount);
        Assert.AreEqual(1, allowed.KillCount);
        Assert.IsTrue(GameLifeCycle.IsGameRunningProperty.Value);
        CollectionAssert.AreEqual(new[] { "Run Snap Hutao as administrator" }, Win32.HutaoNative.Errors);
        Assert.IsTrue(ProcessFactory.Handles.All(static handle => handle.IsDisposed));
    }

    [TestMethod]
    public async Task EndingTheGameRefreshesAnObserverStillWaitingOnAnOlderProcess()
    {
        FakeProcess previouslyObserved = new();
        FakeProcess current = new() { KillError = new Win32Exception(5) };
        ProcessFactory.Current = previouslyObserved;
        using CancellationTokenSource stop = new();
        Task observing = GameLifeCycle.ObserveGameRunningAsync(new TaskContext(), stop.Token);
        try
        {
            await WaitUntilAsync(() => previouslyObserved.ExitWaitCount > 0);
            ProcessFactory.Current = current;
            Assert.IsFalse(await GameLifeCycle.TryKillGameProcessAsync(new TaskContext()));
            await WaitUntilAsync(() => current.ExitWaitCount > 0);
            Assert.IsTrue(GameLifeCycle.IsGameRunningProperty.Value);
        }
        finally
        {
            stop.Cancel();
            await observing.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [TestMethod]
    public async Task WaitingForGameExitDoesNotSpinEnumerateProcesses()
    {
        FakeProcess process = new();
        ProcessFactory.Current = process;
        Task waiting = GameLifeCycle.SpinWaitGameExitAsync(new TaskContext()).AsTask();
        await WaitUntilAsync(static () => ProcessFactory.DiscoveryCount > 0);
        int discoveries = ProcessFactory.DiscoveryCount;
        await Task.Delay(150);
        Assert.AreEqual(discoveries, ProcessFactory.DiscoveryCount);
        process.Exit();
        await waiting.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsFalse(GameLifeCycle.IsGameRunningProperty.Value);
        Assert.IsTrue(ProcessFactory.Handles.All(static handle => handle.IsDisposed));
    }

    [TestMethod]
    public async Task CancelledPageDoesNotPublishAnotherGameState()
    {
        GameLifeCycle.IsGameRunningProperty.Value = true;
        using CancellationTokenSource stop = new();
        stop.Cancel();
        await GameLifeCycle.ObserveGameRunningAsync(new TaskContext(), stop.Token);
        Assert.AreEqual(0, ProcessFactory.DiscoveryCount);
        Assert.IsTrue(GameLifeCycle.IsGameRunningProperty.Value);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        while (!condition()) { await Task.Delay(10, timeout.Token); }
    }

    private sealed class FakeProcess : IProcess
    {
        private readonly TaskCompletionSource exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int exitWaitCount;
        public bool ExitOnKill { get; set; } = true;
        public Exception? KillError { get; set; }
        public int KillCount { get; private set; }
        public int ExitWaitCount => Volatile.Read(ref exitWaitCount);
        public bool HasExited { get; private set; }
        public void Kill()
        {
            KillCount++;
            if (KillError is not null) { throw KillError; }
            if (ExitOnKill) { Exit(); }
        }
        public void Exit() { HasExited = true; exited.TrySetResult(); }
        public Task WaitForExitAsync(CancellationToken token = default)
        {
            Interlocked.Increment(ref exitWaitCount);
            return exited.Task.WaitAsync(token);
        }
        public void Dispose() { }
    }

    private sealed class TaskContext : ITaskContext
    {
        public UserTestThreadSwitch SwitchToMainThreadAsync() => new(static () => true, static action => action());
        public UserTestThreadSwitch SwitchToBackgroundAsync() => new(static () => true, static action => action());
    }
}
