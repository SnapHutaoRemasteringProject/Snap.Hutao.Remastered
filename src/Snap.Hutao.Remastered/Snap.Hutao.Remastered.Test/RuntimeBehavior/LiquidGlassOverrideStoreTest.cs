using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class LiquidGlassOverrideStoreTest
{
    private static readonly LiquidGlassParameterRange[] Ranges = [new("BlurRadius", 0, 32, 18)];

    [TestMethod]
    public void CustomValueSurvivesSerializationAndReload()
    {
        LiquidGlassOverrideStore store = new("{}", Ranges);
        Assert.IsTrue(store.TrySet("BlurRadius", 24));
        LiquidGlassOverrideStore restored = new(store.Serialize(), Ranges);
        Assert.AreEqual(24D, restored.Get("BlurRadius"));
    }

    [TestMethod]
    public void ResetIsLocalToThisControlType()
    {
        LiquidGlassOverrideStore button = new("{}", Ranges);
        LiquidGlassOverrideStore slider = new("{}", Ranges);
        button.TrySet("BlurRadius", 22);
        slider.TrySet("BlurRadius", 26);
        button.Reset();
        Assert.AreEqual(18D, button.Get("BlurRadius"));
        Assert.AreEqual(26D, slider.Get("BlurRadius"));
        Assert.AreEqual("{}", button.Serialize());
    }

    [TestMethod]
    public void NonFiniteOutOfRangeAndUnknownValuesAreRejected()
    {
        LiquidGlassOverrideStore store = new("{\"BlurRadius\":99,\"Unknown\":1}", Ranges);
        Assert.AreEqual(18D, store.Get("BlurRadius"));
        Assert.IsFalse(store.TrySet("BlurRadius", double.NaN));
        Assert.IsFalse(store.TrySet("BlurRadius", double.PositiveInfinity));
        Assert.IsFalse(store.TrySet("BlurRadius", -1));
        Assert.IsFalse(store.TrySet("Unknown", 1));
    }

    [TestMethod]
    public void DefaultValueRemovesOnlyItsOverrideAndCorruptDataIsSafe()
    {
        LiquidGlassOverrideStore store = new("invalid json", Ranges);
        Assert.AreEqual(18D, store.Get("BlurRadius"));
        store.TrySet("BlurRadius", 22);
        store.TrySet("BlurRadius", 18);
        Assert.AreEqual("{}", store.Serialize());
    }
}
