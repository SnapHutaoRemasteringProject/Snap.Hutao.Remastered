using Microsoft.Extensions.AI;
using Snap.Hutao.Remastered.Service.AI.Migration;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Observables;
using Snap.Hutao.Remastered.Service.AI.Settings;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class AgentMigrationTest
{
    [TestMethod]
    public void LegacyConversationRetainsSnapshotDraftAndDisplayWhileExcludingInterruptedAnswersFromReplay()
    {
        AgentSettings settings = new();
        Guid id = Guid.NewGuid();
        settings.ModelProviderProfiles.Add(AgentLegacyMigration.ConvertProfile(new() { Id = id, Model = "model", Name = "provider" }));
        settings.ModelProviderProfiles.MoveCurrentToFirst();
        LegacyConversation legacy = new()
        {
            Id = Guid.NewGuid(), ProfileId = id, Snapshot = "{\"count\":42}", InputText = "draft", Title = "chat",
            Messages = [new() { Content = "first", Completed = true }, new() { Role = "assistant", Content = "answer", Reasoning = "reason", Completed = true },
                new() { Content = "second", Completed = true }, new() { Role = "assistant", Content = "partial", Error = "stopped" }],
        };
        AgentConversation migrated = AgentLegacyMigration.ConvertConversation(legacy, settings);
        Assert.AreEqual(legacy.Id, migrated.Id);
        Assert.AreEqual(legacy.Snapshot, migrated.AccountSnapshot);
        Assert.AreEqual("draft", migrated.InputText);
        Assert.AreEqual(4, migrated.Messages.Count);
        Assert.AreEqual(3, migrated.SeedMessages!.Count);
        Assert.IsTrue(migrated.Messages[1].Contents.OfType<ObservableTextReasoningContent>().Any());
        Assert.IsTrue(migrated.Messages[^1].Contents.OfType<ObservableErrorContent>().Any());
        Assert.AreEqual(id, migrated.ModelProviderProfileId);
        Assert.AreEqual(id, migrated.ModelProfileId);
        Assert.IsFalse(migrated.SeedMessages.Any(message => message.Text.Contains("partial", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void LegacyEndpointsAndModelOptionsAreConvertedToSdkBaseUrls()
    {
        ModelProviderProfile profile = AgentLegacyMigration.ConvertProfile(new()
        {
            Id = Guid.NewGuid(), Model = "test", Endpoint = "https://example.com/v1/chat/completions?api-version=1",
            Temperature = 0.5, MaxOutputTokens = 8192, ReasoningEffort = "high",
        });
        Assert.AreEqual("https://example.com/v1?api-version=1", profile.Endpoint);
        Assert.AreEqual(0.5F, profile.ModelProfiles[0].AgentOptions.Temperature);
        Assert.AreEqual(8192, profile.ModelProfiles[0].AgentOptions.MaxOutputTokens);
        ModelProviderProfile anthropic = AgentLegacyMigration.ConvertProfile(new() { Provider = 2, Endpoint = "https://example.com/v1/messages" });
        Assert.AreEqual("https://example.com", anthropic.Endpoint);
    }

    [TestMethod]
    public void ProviderJsonNeverContainsApiKeys()
    {
        ModelProviderProfile profile = new() { Name = "provider", ApiKey = "test-secret-value" };
        string json = JsonSerializer.Serialize(profile);
        Assert.IsFalse(json.Contains("test-secret-value", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("ApiKey", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("provider", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RegenerationCheckpointRoundTripsFrameworkMessageAndState()
    {
        using JsonDocument document = JsonDocument.Parse("{\"session\":42}");
        AgentTurnCheckpoint checkpoint = new()
        {
            InputMessage = new ChatMessage(ChatRole.User, "question"), MessageCount = 3, SerializedSessionState = document.RootElement.Clone(),
        };
        JsonSerializerOptions options = new(AIJsonUtilities.DefaultOptions);
        AgentTurnCheckpoint restored = JsonSerializer.Deserialize<AgentTurnCheckpoint>(JsonSerializer.Serialize(checkpoint, options), options)!;
        Assert.AreEqual("question", restored.InputMessage.Text);
        Assert.AreEqual(3, restored.MessageCount);
        Assert.AreEqual(42, restored.SerializedSessionState!.Value.GetProperty("session").GetInt32());
    }
}
