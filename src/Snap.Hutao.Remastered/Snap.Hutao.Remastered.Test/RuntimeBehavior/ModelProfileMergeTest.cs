using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.Collections.Specialized;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class ModelProfileMergeTest
{
    [TestMethod]
    public void FetchedModelsArePopulatedBeforeTheyBecomeVisible()
    {
        ObservableSettingsCollection<ModelProfile, Guid> destination = new();
        destination.CollectionChanged += (_, args) =>
        {
            if (args.Action != NotifyCollectionChangedAction.Add) { return; }
            ModelProfile added = (ModelProfile)args.NewItems![0]!;
            Assert.IsFalse(string.IsNullOrWhiteSpace(added.ModelId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(added.DisplayName));
        };
        ModelProfileMerger.Merge(destination, [new() { ModelId = "qwen3.8-flash" }, new() { ModelId = "qwen-plus", Name = "Plus" }]);
        Assert.HasCount(2, destination);
        Assert.AreEqual("qwen3.8-flash", destination[0].DisplayName);
        Assert.AreSame(destination[0], destination.CurrentItem);
    }

    [TestMethod]
    public void RefreshKeepsIdentityAliasesAndOptionsWhileDeduplicatingIds()
    {
        ModelProfile existing = new() { ModelId = "qwen3.8-flash", Name = "My fast model" };
        existing.AgentOptions.MaxOutputTokens = 2048;
        ObservableSettingsCollection<ModelProfile, Guid> destination = [existing];
        destination.CurrentItem = existing;
        Guid id = existing.Id;
        ModelProfileMerger.Merge(destination, [new() { ModelId = " qwen3.8-flash ", Name = "Server name" }, new() { ModelId = "qwen3.8-flash" }, new() { ModelId = " " }]);
        Assert.HasCount(1, destination);
        Assert.AreSame(existing, destination[0]);
        Assert.AreEqual(id, existing.Id);
        Assert.AreEqual("My fast model", existing.DisplayName);
        Assert.AreEqual(2048, existing.AgentOptions.MaxOutputTokens);
        Assert.AreSame(existing, destination.CurrentItem);
    }
}
