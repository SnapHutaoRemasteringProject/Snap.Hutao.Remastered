// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Game.Island;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class IslandFrameRateTest
{
    [TestMethod]
    public void ReturningToGameRestoresForegroundFrameRate()
    {
        int background = IslandFrameRate.SelectTarget(true, 144, true, 25, 100, 200);
        int foreground = IslandFrameRate.SelectTarget(true, 144, true, 25, 100, 100);

        Assert.AreEqual(25, background);
        Assert.AreEqual(144, foreground);
    }

    [TestMethod]
    [DataRow(false, 25, 100, 200)]
    [DataRow(true, 25, 100, 0)]
    [DataRow(true, 25, 0, 200)]
    [DataRow(true, 0, 100, 200)]
    [DataRow(true, 2001, 100, 200)]
    public void KeepsForegroundSettingsWhenBackgroundLimitDoesNotApply(
        bool backgroundEnabled, int backgroundTargetFps, int gameProcessId, int foregroundProcessId)
    {
        int result = IslandFrameRate.SelectTarget(true, 144, backgroundEnabled, backgroundTargetFps, gameProcessId, (uint)foregroundProcessId);

        Assert.AreEqual(144, result);
    }

    [TestMethod]
    public void BackgroundLimitRespectsFrameRateSwitch()
    {
        int result = IslandFrameRate.SelectTarget(false, 144, true, 25, 100, 200);

        Assert.AreEqual(144, result);
    }
}
