using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class BackdropOverrideStateTest
{
    [TestMethod]
    public void LeavingGlassRestoresThePreviousMaterial()
    {
        BackdropOverrideState<string> native = new(false, "unused", "Acrylic");
        BackdropOverrideState<string> glass = native.Select(true, "None");
        Assert.AreEqual(new(true, "Acrylic", "None"), glass);
        Assert.AreEqual(new(false, "Acrylic", "Acrylic"), glass.Select(false, "None"));
    }

    [TestMethod]
    public void ReloadingAnActiveOverrideDoesNotReplaceItsSavedMaterialWithNone()
    {
        // These three fields are restored from the persisted settings on startup.
        BackdropOverrideState<string> restored = new(true, "Mica", "None");
        Assert.AreEqual(restored, restored.Select(true, "None"));
        Assert.AreEqual("Mica", restored.Select(true, "None").Select(false, "None").Current);
    }

    [TestMethod]
    public void AnExplicitNonePreferenceIsPreserved()
    {
        BackdropOverrideState<string> native = new(false, "old", "None");
        Assert.AreEqual("None", native.Select(true, "None").Select(false, "None").Current);
    }

    [TestMethod]
    public void DefaultStartupDoesNotRestoreAnInactiveSnapshot()
    {
        BackdropOverrideState<string> native = new(false, "Acrylic", "Mica");
        Assert.AreEqual(native, native.Select(false, "None"));
    }
}
