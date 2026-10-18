using Snap.Hutao.Remastered.Service.BackgroundMediaPlayer;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class BackgroundMediaPlaybackControllerTest
{
    [TestMethod]
    public void LockPausesVisiblePlaybackAndUnlockResumesIt()
    {
        List<bool> playback = [];
        BackgroundMediaPlaybackController controller = new(playback.Add);
        controller.SetWindowVisible(true);
        controller.SetSessionLocked(true);
        controller.SetSessionLocked(false);
        CollectionAssert.AreEqual(new[] { true, false, true }, playback);
    }

    [TestMethod]
    public void UnlockDoesNotResumeAMinimizedOrHiddenWindow()
    {
        List<bool> playback = [];
        BackgroundMediaPlaybackController controller = new(playback.Add);
        controller.SetWindowVisible(true);
        controller.SetSessionLocked(true);
        controller.SetWindowVisible(false);
        controller.SetSessionLocked(false);
        Assert.IsFalse(playback[^1]);
        controller.SetWindowVisible(true);
        Assert.IsTrue(playback[^1]);
    }

    [TestMethod]
    public void RestoringTheWindowWhileLockedDoesNotResumePlayback()
    {
        List<bool> playback = [];
        BackgroundMediaPlaybackController controller = new(playback.Add);
        controller.SetSessionLocked(true);
        controller.SetWindowVisible(true);
        Assert.IsFalse(playback[^1]);
        controller.SetSessionLocked(false);
        Assert.IsTrue(playback[^1]);
    }

    [TestMethod]
    public void SourceCompletionAndPeriodicRefreshRecheckTheLockState()
    {
        List<bool> playback = [];
        BackgroundMediaPlaybackController controller = new(playback.Add);
        controller.SetWindowVisible(true);
        controller.SetSessionLocked(true);
        // The media service reapplies this policy after an awaited source load.
        controller.Apply();
        controller.Apply();
        CollectionAssert.AreEqual(new[] { true, false, false, false }, playback);
    }

    [TestMethod]
    public void AnExplicitPauseSurvivesUnlockAndWindowRestore()
    {
        List<bool> playback = [];
        BackgroundMediaPlaybackController controller = new(playback.Add);
        controller.SetWindowVisible(true);
        controller.SetPlaybackRequested(false);
        controller.SetSessionLocked(true);
        controller.SetWindowVisible(false);
        controller.SetSessionLocked(false);
        controller.SetWindowVisible(true);
        controller.Apply();
        Assert.IsFalse(playback[^1]);
        controller.SetPlaybackRequested(true);
        Assert.IsTrue(playback[^1]);
    }

    [TestMethod]
    public void PlayCannotOverrideASessionLockOrAnUnloadedView()
    {
        List<bool> playback = [];
        BackgroundMediaPlaybackController controller = new(playback.Add);
        controller.SetPlaybackRequested(true);
        Assert.IsFalse(playback[^1]);
        controller.SetWindowVisible(true);
        controller.SetSessionLocked(true);
        controller.SetPlaybackRequested(true);
        Assert.IsFalse(playback[^1]);
        controller.SetSessionLocked(false);
        controller.SetWindowVisible(false);
        controller.Apply();
        Assert.IsFalse(playback[^1]);
    }
}
