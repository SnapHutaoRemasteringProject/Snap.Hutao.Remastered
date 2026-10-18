using Snap.Hutao.Remastered.ViewModel.Guide;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class GuideNavigationTest
{
    [TestMethod]
    public void FailedTransfersReturnToResourceSelection()
    {
        Assert.AreEqual(GuideState.StaticResourceSetting, GuideNavigation.StateAfterDownload(false, 1));
        Assert.AreEqual(GuideState.StaticResourceSetting, GuideNavigation.StateAfterDownload(false, 20));
    }

    [TestMethod]
    public void SuccessfulTransfersContinueToMaterialSelection()
    {
        Assert.AreEqual(GuideState.Material, GuideNavigation.StateAfterDownload(false, 0));
    }

    [TestMethod]
    public void AnExplicitSkipCanContinueWithOutstandingTransfers()
    {
        Assert.AreEqual(GuideState.Material, GuideNavigation.StateAfterDownload(true, 20));
    }

    [TestMethod]
    public void ChoosingImageResourcesIsNotTheFinalStep()
    {
        Assert.IsFalse(GuideNavigation.IsFinalStep((uint)GuideState.StaticResourceSetting));
        Assert.IsFalse(GuideNavigation.IsFinalStep((uint)GuideState.StaticResourceBegin));
        Assert.IsTrue(GuideNavigation.IsFinalStep((uint)GuideState.Material));
    }
}
