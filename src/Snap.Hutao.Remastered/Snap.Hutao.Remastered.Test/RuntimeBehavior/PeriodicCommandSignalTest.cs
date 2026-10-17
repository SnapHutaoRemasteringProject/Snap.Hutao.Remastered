using Snap.Hutao.Remastered.UI.Xaml.Behavior;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class PeriodicCommandSignalTest
{
    [TestMethod]
    public async Task StopCompletesAPendingWaitWithoutThrowingCancellation()
    {
        using PeriodicCommandSignal signal = new(TimeSpan.FromHours(1));
        Task<bool> pending = signal.WaitAsync();
        signal.Dispose();
        Assert.IsFalse(await pending.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.IsFalse(await signal.WaitAsync());
    }

    [TestMethod]
    public async Task ThemeChangesCanWakeRepeatedlyWithoutReplacingOrCancelingTheTimer()
    {
        using PeriodicCommandSignal signal = new(TimeSpan.FromHours(1));
        for (int index = 0; index < 3; index++)
        {
            Task<bool> pending = signal.WaitAsync();
            signal.NotifyThemeChanged();
            Assert.IsTrue(await pending.WaitAsync(TimeSpan.FromSeconds(2)));
        }

        Task<bool> last = signal.WaitAsync();
        signal.Dispose();
        Assert.IsFalse(await last.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [TestMethod]
    public async Task ThemeSignalsAreCoalescedAndStopIsIdempotent()
    {
        using PeriodicCommandSignal signal = new(TimeSpan.FromHours(1));
        signal.NotifyThemeChanged();
        signal.NotifyThemeChanged();
        Assert.IsTrue(await signal.WaitAsync());
        Task<bool> pending = signal.WaitAsync();
        Assert.IsFalse(pending.IsCompleted);
        signal.Dispose();
        signal.Dispose();
        signal.NotifyThemeChanged();
        Assert.IsFalse(await pending.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [TestMethod]
    public async Task AReattachedBehaviorCanUseAnIndependentNewSignal()
    {
        using PeriodicCommandSignal oldRun = new(TimeSpan.FromHours(1));
        oldRun.Dispose();
        using PeriodicCommandSignal newRun = new(TimeSpan.FromHours(1));
        newRun.NotifyThemeChanged();
        Assert.IsTrue(await newRun.WaitAsync());
        Assert.IsFalse(await oldRun.WaitAsync());
    }

    [TestMethod]
    public async Task PeriodicTickStillFires()
    {
        using PeriodicCommandSignal signal = new(TimeSpan.FromMilliseconds(10));
        Assert.IsTrue(await signal.WaitAsync().WaitAsync(TimeSpan.FromSeconds(2)));
    }
}
