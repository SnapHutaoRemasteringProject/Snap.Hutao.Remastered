using Snap.Hutao.Remastered.UI.Xaml.Control;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class DesiredSizeRestrictionTest
{
    [TestMethod]
    public void UnboundedScrollMeasurementKeepsOnlyIntrinsicHeight()
    {
        double retained = 0;
        Assert.AreEqual(32D, DesiredSizeRestriction.Apply(32, double.PositiveInfinity, true, ref retained));
        Assert.AreEqual(32D, retained);
        Assert.AreEqual(32D, DesiredSizeRestriction.Apply(24, double.PositiveInfinity, true, ref retained));
    }

    [TestMethod]
    public void SmallerAvailableExtentCanReleaseTheOldMinimum()
    {
        double retained = 900;
        Assert.AreEqual(32D, DesiredSizeRestriction.Apply(32, 100, true, ref retained));
        Assert.AreEqual(32D, retained);
    }

    [TestMethod]
    public void DisabledOrNonFiniteDesiredExtentIsNeverCached()
    {
        double retained = 900;
        Assert.AreEqual(32D, DesiredSizeRestriction.Apply(32, double.PositiveInfinity, false, ref retained));
        Assert.AreEqual(0D, retained);
        Assert.AreEqual(double.PositiveInfinity, DesiredSizeRestriction.Apply(double.PositiveInfinity, double.PositiveInfinity, true, ref retained));
        Assert.AreEqual(0D, retained);
    }
}
