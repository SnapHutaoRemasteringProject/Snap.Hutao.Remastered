using Snap.Hutao.Remastered.Service.Game.Package.Advanced;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class PredownloadProgressTrackerTest
{
    [TestMethod]
    public void ResetImmediatelyPublishesAnUnfinishedDownload()
    {
        PredownloadProgressTracker progress = new();
        progress.Reset(4, 1000);
        Assert.AreEqual(new PredownloadProgressSnapshot(false, 0, 4, 0), progress.Snapshot);
    }

    [TestMethod]
    public void ByteReportsAdvanceBeforeTheFirstBlockCompletes()
    {
        PredownloadProgressTracker progress = new();
        progress.Reset(4, 1000);
        progress.Download(500, 0);
        Assert.AreEqual(50, progress.Snapshot.Percent);
        Assert.AreEqual(0, progress.Snapshot.DownloadedBlocks);
    }

    [TestMethod]
    public void CachedBlockReportsAlsoAdvanceProgress()
    {
        PredownloadProgressTracker progress = new();
        progress.Reset(4, 1000);
        progress.Download(0, 1);
        Assert.AreEqual(25, progress.Snapshot.Percent);
    }

    [TestMethod]
    public void ProgressStaysBelowOneHundredUntilTheFinishReport()
    {
        PredownloadProgressTracker progress = new();
        progress.Reset(4, 1000);
        progress.Download(long.MaxValue, int.MaxValue);
        Assert.AreEqual(99, progress.Snapshot.Percent);
        Assert.IsFalse(progress.Snapshot.Finished);
        progress.Complete();
        Assert.AreEqual(new PredownloadProgressSnapshot(true, 4, 4, 100), progress.Snapshot);
    }

    [TestMethod]
    public void RetryResetsTheOldProgressAndEmptyDownloadsCanComplete()
    {
        PredownloadProgressTracker progress = new();
        progress.Reset(4, 1000);
        progress.Download(500, 2);
        progress.Reset(0, 0);
        Assert.AreEqual(default(PredownloadProgressSnapshot), progress.Snapshot);
        progress.Complete();
        Assert.IsTrue(progress.Snapshot.Finished);
        Assert.AreEqual(100, progress.Snapshot.Percent);
    }

    [TestMethod]
    public void IncompleteDiskSnapshotsNeverClaimOneHundredPercent()
    {
        Assert.AreEqual(99, PredownloadProgressSnapshot.FromBlocks(false, 8, 8).Percent);
        Assert.AreEqual(100, PredownloadProgressSnapshot.FromBlocks(true, 8, 8).Percent);
    }
}
