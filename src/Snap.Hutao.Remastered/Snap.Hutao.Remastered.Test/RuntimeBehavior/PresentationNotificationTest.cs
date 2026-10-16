using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Service.Game.PathAbstraction;
using Snap.Hutao.Remastered.ViewModel.AvatarProperty;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class PresentationNotificationTest
{
    [TestMethod]
    public void BaseAndDerivedPropertiesUseTheSameNotificationEvent()
    {
        TestEquipment view = new();
        List<string?> changes = [];
        view.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        view.Name = "Sword";
        view.Description = "Description";
        view.Icon = new Uri("https://example.test/icon.png");

        CollectionAssert.AreEqual(new[] { "Name", "Description", "Icon" }, changes);
    }

    [TestMethod]
    public void ReassigningTheSameValueDoesNotRaiseANotification()
    {
        NameDescription view = new("Name", "Description");
        int count = 0;
        view.PropertyChanged += (_, _) => count++;
        view.Name = "Name";
        view.Description = "Description";
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void ChangingASpecialParameterNotifiesItsBinding()
    {
        ParameterDescription view = new("Parameter", "Description");
        List<string?> changes = [];
        view.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        view.IsSpecial = true;
        CollectionAssert.AreEqual(new[] { "IsSpecial" }, changes);
    }

    [TestMethod]
    public void InitOnlyPropertiesHaveARealSetterWithoutChangingTheirPublicContract()
    {
        GamePathEntry view = GamePathEntry.Create("old");
        List<string?> changes = [];
        view.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        // Exercise the CLR setter used by deserializers, retaining C# init-only
        // access. This also guards against an empty INotifyPropertyChanged stub.
        typeof(GamePathEntry).GetProperty(nameof(GamePathEntry.Path))!.SetValue(view, "new");
        Assert.AreEqual("new", view.Path);
        CollectionAssert.AreEqual(new[] { "Path" }, changes);
    }

    private sealed class TestEquipment : NameIconDescription
    {
    }
}
