using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Snap.Hutao.Remastered.Core.AI.Text.Json;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Observables;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class AgentConversationJsonTest
{
    [TestMethod]
    public void ConversationRestoresMessagesApprovalAndRetryCheckpointWithoutReflectionResolver()
    {
        JsonSerializerOptions options = ProductionOptions();
        Assert.AreSame(AgentConversationJsonContext.Default, JsonSerialize.GetTypeInfo<AgentConversation>(options).OriginatingResolver);
        AgentConversation expected = Conversation(options);
        string json = JsonSerialize.Serialize(expected, options);
        AgentConversation actual = JsonSerialize.Deserialize<AgentConversation>(json, options)!;

        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.ModelProfileId, actual.ModelProfileId);
        Assert.AreEqual("中文 <&>", actual.Title);
        Assert.AreEqual("input", actual.LastTurnCheckpoint!.InputMessage.Text);
        Assert.AreEqual(2, actual.LastTurnCheckpoint.MessageCount);
        Assert.AreEqual("seed", actual.SeedMessages!.Single().Text);
        Assert.AreEqual("session", actual.SerializedSessionState!.Value.GetProperty("state").GetString());
        Assert.AreEqual("answer", ((ObservableTextContent)actual.Messages.Single().Contents[0]).Text);
        Assert.AreEqual(32L, ((ObservableUsageContent)actual.Messages.Single().Contents[1]).TotalTokenCount);
        ObservableToolApprovalRequestContent approval = actual.ToolApprovalRequests.Single();
        Assert.IsTrue(approval.CanRespond);
        Assert.AreEqual("approval-1", approval.RawRepresentation!.RequestId);
        Assert.AreEqual("GetCurrentTime", ((FunctionCallContent)approval.RawRepresentation.ToolCall).Name);
        Assert.AreEqual(expected.Messages.Single().Id, approval.TargetMessageId);
        using JsonDocument arguments = JsonDocument.Parse(((ObservableFunctionCallContent)approval.ToolCall!).Arguments!);
        Assert.AreEqual("Asia/Shanghai", arguments.RootElement.GetProperty("zone").GetString());
        Assert.IsTrue(approval.RawRepresentation.CreateResponse(true).Approved);
        Assert.AreEqual(json, JsonSerialize.Serialize(actual, options));
    }

    [TestMethod]
    public void GeneratedConversationPreservesExistingFileFormatInBothDirections()
    {
        JsonSerializerOptions generated = ProductionOptions();
        JsonSerializerOptions previous = new(AIJsonUtilities.DefaultOptions)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };
        AgentConversation expected = Conversation(previous);
        string oldJson = JsonSerializer.Serialize(expected, previous);
        Assert.AreEqual(oldJson, JsonSerialize.Serialize(expected, generated));
        AgentConversation restored = JsonSerialize.Deserialize<AgentConversation>(oldJson, generated)!;
        Assert.AreEqual(oldJson, JsonSerializer.Serialize(restored, previous));
        Assert.AreEqual(oldJson, JsonSerialize.Serialize(JsonSerializer.Deserialize<AgentConversation>(oldJson, previous), generated));
    }

    [TestMethod]
    public void GeneratedConversationRejectsInvalidCheckpointAndKeepsExistingNumberPolicy()
    {
        JsonSerializerOptions options = ProductionOptions();
        Assert.ThrowsExactly<JsonException>(() => JsonSerialize.Deserialize<AgentConversation>("""{"LastTurnCheckpoint":{}}""", options));
        AgentConversation actual = JsonSerialize.Deserialize<AgentConversation>("""{"lastTurnCheckpoint":{"inputMessage":{"role":"user","contents":[]},"messageCount":"2"}}""", options)!;
        Assert.AreEqual(2, actual.LastTurnCheckpoint!.MessageCount);
    }

    private static JsonSerializerOptions ProductionOptions()
    {
        using ServiceProvider provider = new ServiceCollection().AddJsonSerializerOptions().BuildServiceProvider();
        return provider.GetRequiredKeyedService<JsonSerializerOptions>(JsonSerializerOptionsKey.AgentConversation);
    }

    private static AgentConversation Conversation(JsonSerializerOptions options)
    {
        using JsonDocument state = JsonDocument.Parse("""{"state":"session","opaque":[1,true,null]}""");
        AgentConversation conversation = new()
        {
            Title = "中文 <&>",
            ModelProfileId = Guid.NewGuid(),
            ModelProviderProfileId = Guid.NewGuid(),
            SeedMessages = [new(ChatRole.System, "seed")],
            SerializedSessionState = state.RootElement.Clone(),
            LastTurnCheckpoint = new()
            {
                InputMessage = new(ChatRole.User, "input"),
                MessageCount = 2,
                TitleInput = "retry",
                SeedMessages = [new(ChatRole.System, "seed")],
                SerializedSessionState = state.RootElement.Clone(),
            },
        };
        ObservableChatMessage message = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.UtcNow, content: ObservableTextContent.Create("answer"));
        message.Contents.Add(new ObservableUsageContent { TotalTokenCount = 32 });
        conversation.Messages.Add(message);
        ToolApprovalRequestContent request = new("approval-1", new FunctionCallContent("call-1", "GetCurrentTime", new Dictionary<string, object?> { ["zone"] = "Asia/Shanghai" }));
        ObservableToolApprovalRequestContent approval = ObservableToolApprovalRequestContent.Create(request, options);
        approval.TargetMessageId = message.Id;
        conversation.ToolApprovalRequests.Add(approval);
        conversation.ToolApprovalRequest = approval;
        return conversation;
    }
}
