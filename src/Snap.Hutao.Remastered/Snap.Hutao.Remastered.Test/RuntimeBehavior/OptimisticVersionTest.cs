// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Threading;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class OptimisticVersionTest
{
    [TestMethod]
    public void AdvanceInvalidatesPreviouslyCapturedTokens()
    {
        OptimisticVersion version = new();
        OptimisticVersionToken before = version.Capture();
        Assert.IsTrue(before.IsCurrent);

        OptimisticVersionToken after = version.Advance();

        Assert.IsFalse(before.IsCurrent);
        Assert.IsTrue(after.IsCurrent);
        Assert.IsFalse(default(OptimisticVersionToken).IsCurrent);
    }

    [TestMethod]
    public void OnlyTheLatestReplacementRemainsCurrent()
    {
        OptimisticVersion version = new();
        OptimisticVersionToken first = version.Advance();
        OptimisticVersionToken second = version.Advance();

        Assert.IsFalse(first.IsCurrent);
        Assert.IsTrue(second.IsCurrent);
    }
}
