using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Observables;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.Runtime.CompilerServices;
using System.Text;
using System.ClientModel.Primitives;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class AgentHarnessTest
{
    [TestMethod]
    public async Task ApprovalSurvivesSerializedSessionAndExecutesOnlyAfterApproval()
    {
        int calls = 0;
        AIFunction function = AIFunctionFactory.Create(() => { calls++; return "time"; }, "GetCurrentTime");
        await VerifyApprovalAsync(new ApprovalRequiredAIFunction(function), true);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public async Task DeniedToolDoesNotExecute()
    {
        int calls = 0;
        AIFunction function = AIFunctionFactory.Create(() => { calls++; return "time"; }, "GetCurrentTime");
        await VerifyApprovalAsync(new ApprovalRequiredAIFunction(function), false);
        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public async Task ChatCompletionsToolApprovalSurvivesSerializedSession()
    {
        int calls = 0;
        AIFunction function = AIFunctionFactory.Create(() => { calls++; return "time"; }, "GetCurrentTime");
        await VerifyApprovalAsync(new ApprovalRequiredAIFunction(function), true, ModelProviderType.OpenAIChatCompletion);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void ObservableSegmentsAndPendingApprovalRoundTripWithFrameworkJsonOptions()
    {
        JsonSerializerOptions options = new(AIJsonUtilities.DefaultOptions);
        ToolApprovalRequestContent request = new("approval-1", new FunctionCallContent("call-1", "GetCurrentTime", new Dictionary<string, object?>()));
        ObservableToolApprovalRequestContent observable = ObservableToolApprovalRequestContent.Create(request, options);
        string json = JsonSerializer.Serialize(observable, options);
        ObservableToolApprovalRequestContent restored = JsonSerializer.Deserialize<ObservableToolApprovalRequestContent>(json, options)!;
        Assert.IsTrue(restored.CanRespond);
        Assert.AreEqual("approval-1", restored.RawRepresentation!.RequestId);
        Assert.AreEqual("call-1", ((FunctionCallContent)restored.RawRepresentation.ToolCall).CallId);
        Assert.IsTrue(restored.RawRepresentation.CreateResponse(true).Approved);
    }

    [TestMethod]
    public async Task ChatCompletionsReasoningRoundTripsAcrossPersistedSessions()
    {
        using ServiceProvider services = new ServiceCollection().AddLogging()
            .AddSingleton<ObjectPool<StringBuilder>>(new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy())).BuildServiceProvider();
        ScriptedClient client = new() { ReasoningMode = true };
        ExtendedAgentOptions options = new() { ModelId = "test", MaxOutputTokens = 256, MaxContextWindowTokens = 4096 };
        HarnessAgent agent = options.CreateHarnessAgent(client, [], services);
        AgentSession session = await agent.CreateSessionAsync();
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, "first")], session)) { }
        JsonSerializerOptions serializerOptions = new(AIJsonUtilities.DefaultOptions);
        JsonElement serialized = await agent.SerializeSessionAsync(session, serializerOptions);
        AgentSession restored = await agent.DeserializeSessionAsync(serialized, serializerOptions);
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, "second")], restored)) { }
        ChatMessage previous = client.LastMessages.Last(message => message.Role == ChatRole.Assistant);
        Assert.IsTrue(previous.Contents.OfType<TextReasoningContent>().Any(content => content.Text == "reasoning"));
        Assert.IsInstanceOfType<OpenAI.Chat.ChatMessage>(previous.RawRepresentation);
        BinaryData raw = ModelReaderWriter.Write((OpenAI.Chat.ChatMessage)previous.RawRepresentation!, ModelReaderWriterOptions.Json);
        using JsonDocument json = JsonDocument.Parse(raw);
        Assert.AreEqual("reasoning", json.RootElement.GetProperty("reasoning_content").GetString());
    }

    [TestMethod]
    public async Task ResponsesKeepOpaqueReasoningDataInSerializedSessions()
    {
        using ServiceProvider services = new ServiceCollection().AddLogging()
            .AddSingleton<ObjectPool<StringBuilder>>(new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy())).BuildServiceProvider();
        ScriptedClient client = new() { ReasoningMode = true };
        ExtendedAgentOptions options = new() { ProviderType = ModelProviderType.OpenAIResponses, ModelId = "test", MaxOutputTokens = 256, MaxContextWindowTokens = 4096 };
        HarnessAgent agent = options.CreateHarnessAgent(client, [], services);
        AgentSession session = await agent.CreateSessionAsync();
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, "first")], session)) { }
        JsonSerializerOptions serializerOptions = new(AIJsonUtilities.DefaultOptions);
        AgentSession restored = await agent.DeserializeSessionAsync(await agent.SerializeSessionAsync(session, serializerOptions), serializerOptions);
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, "second")], restored)) { }
        TextReasoningContent reasoning = client.LastMessages.Last(message => message.Role == ChatRole.Assistant).Contents.OfType<TextReasoningContent>().Single();
        Assert.AreEqual("opaque-reasoning", reasoning.ProtectedData);
    }

    [TestMethod]
    public void OptionsDiagnosticsDoNotExposeApiKeysOrStatistics()
    {
        ExtendedAgentOptions options = new() { ModelId = "test", ApiKey = "private-key", SystemPrompt = "private-statistics" };
        Assert.IsFalse(options.ToString().Contains("private-key", StringComparison.Ordinal));
        Assert.IsFalse(options.ToString().Contains("private-statistics", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ProfileCopyPreservesObjectIdentityAndCopiesModelOptions()
    {
        ModelProviderProfile source = new() { Name = "provider", Endpoint = "https://example.com/v1" };
        ModelProfile model = new() { Name = "model", ModelId = "test" };
        model.AgentOptions.ThinkingEnabled = false;
        source.ModelProfiles.Add(model);
        source.ModelProfiles.CurrentItem = model;
        ModelProviderProfile destination = new();
        destination.CopyFrom(source);
        ModelProfile retained = destination.ModelProfiles[0];
        source.ModelProfiles[0].AgentOptions.MaxOutputTokens = 2048;
        destination.CopyFrom(source);
        Assert.AreSame(retained, destination.ModelProfiles[0]);
        Assert.AreEqual(2048, retained.AgentOptions.MaxOutputTokens);
        Assert.IsFalse(retained.AgentOptions.ThinkingEnabled);
        Assert.AreEqual(model.Id, destination.SelectedModelProfileId);
        Assert.AreNotSame(source.ModelProfiles[0], destination.ModelProfiles[0]);
    }

    [TestMethod]
    public void MultipleTextPartsRetainTheirTrailingBuffer()
    {
        var pool = new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy());
        var provider = new Snap.Hutao.Remastered.Service.AI.Compatibility.OpenAIChatCompletion.RoundTripInMemoryChatHistoryProvider(pool);
        OpenAI.Chat.AssistantChatMessage raw = new([
            OpenAI.Chat.ChatMessageContentPart.CreateTextPart("first"),
            OpenAI.Chat.ChatMessageContentPart.CreateTextPart("second")]);
        typeof(Snap.Hutao.Remastered.Service.AI.Compatibility.OpenAIChatCompletion.RoundTripInMemoryChatHistoryProvider)
            .GetMethod("WorkaroundContentRoundTrip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(provider, [raw]);
        Assert.AreEqual("firstsecond", raw.Content.Single().Text);
    }

    [TestMethod]
    public void MultipleApprovalRequestsAreRetainedAndPersisted()
    {
        JsonSerializerOptions options = new(AIJsonUtilities.DefaultOptions);
        AgentResponseUpdate update = new()
        {
            Contents = [new ToolApprovalRequestContent("approval-1", new FunctionCallContent("call-1", "GetCurrentTime", null)),
                new ToolApprovalRequestContent("approval-2", new FunctionCallContent("call-2", "ShowSummary", null))]
        };
        var extended = Snap.Hutao.Remastered.Service.AI.ExtendedAgentResponseUpdate.Create(update, options);
        Assert.HasCount(2, extended.ToolApprovalRequests);
        extended.ToolApprovalRequests[0].IsHandled = true;
        extended.ToolApprovalRequests[0].Approved = false;
        AgentConversation conversation = new() { ToolApprovalRequests = extended.ToolApprovalRequests };
        AgentConversation restored = JsonSerializer.Deserialize<AgentConversation>(JsonSerializer.Serialize(conversation, options), options)!;
        Assert.HasCount(2, restored.ToolApprovalRequests);
        Assert.IsFalse(restored.ToolApprovalRequests[0].Approved);
        Assert.IsTrue(restored.ToolApprovalRequests[1].CanRespond);
        Assert.AreEqual("approval-2", restored.ToolApprovalRequests[1].RawRepresentation!.CreateResponse(true).RequestId);
    }

    [TestMethod]
    [DataRow("https://example.com/v1", true)]
    [DataRow("http://localhost:8080/v1", true)]
    [DataRow("http://127.0.0.1:8080/v1", true)]
    [DataRow("http://[::1]:8080/v1", true)]
    [DataRow("http://example.com/v1", false)]
    [DataRow("file:///C:/test", false)]
    [DataRow("https://user:password@example.com/v1", false)]
    public void EndpointPolicyRejectsUnencryptedRemoteHosts(string endpoint, bool allowed)
    {
        if (allowed) { Assert.IsNotNull(Snap.Hutao.Remastered.Service.AI.AgentEndpoint.Validate(endpoint)); }
        else { Assert.Throws<ArgumentException>(() => Snap.Hutao.Remastered.Service.AI.AgentEndpoint.Validate(endpoint)); }
    }

    [TestMethod]
    public async Task MultipleToolsResumeWithBothDecisionsAfterRestoringSession()
    {
        int approvedCalls = 0;
        int deniedCalls = 0;
        using ServiceProvider services = new ServiceCollection().AddLogging()
            .AddSingleton<ObjectPool<StringBuilder>>(new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy())).BuildServiceProvider();
        ScriptedClient client = new() { TwoTools = true };
        ExtendedAgentOptions options = new() { ProviderType = ModelProviderType.OpenAIChatCompletion, ModelId = "test", MaxOutputTokens = 256, MaxContextWindowTokens = 4096, MaximumIterationsPerRequest = 4 };
        AITool[] tools = [new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => { approvedCalls++; return "time"; }, "GetCurrentTime")),
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => { deniedCalls++; return "summary"; }, "ShowSummary"))];
        HarnessAgent agent = options.CreateHarnessAgent(client, tools, services);
        AgentSession session = await agent.CreateSessionAsync();
        JsonSerializerOptions jsonOptions = new(AIJsonUtilities.DefaultOptions);
        List<ObservableToolApprovalRequestContent> pending = [];
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, "both tools")], session))
        {
            pending.AddRange(Snap.Hutao.Remastered.Service.AI.ExtendedAgentResponseUpdate.Create(update, jsonOptions).ToolApprovalRequests);
        }
        Assert.HasCount(2, pending);
        Assert.AreEqual(0, approvedCalls + deniedCalls);
        pending[0].IsHandled = true;
        pending[0].Approved = true;
        AgentConversation stored = new() { ToolApprovalRequests = pending };
        stored = JsonSerializer.Deserialize<AgentConversation>(JsonSerializer.Serialize(stored, jsonOptions), jsonOptions)!;
        stored.ToolApprovalRequests[1].Approved = false;
        AgentSession restored = await agent.DeserializeSessionAsync(await agent.SerializeSessionAsync(session, jsonOptions), jsonOptions);
        List<AIContent> responses = stored.ToolApprovalRequests.Select(static request => (AIContent)request.RawRepresentation!.CreateResponse(request.Approved == true)).ToList();
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, responses)], restored)) { }
        Assert.AreEqual(1, approvedCalls);
        Assert.AreEqual(0, deniedCalls);
        Assert.AreEqual(2, client.Requests);
    }

    private static async Task VerifyApprovalAsync(AITool tool, bool approved, ModelProviderType provider = ModelProviderType.OpenAIResponses)
    {
        using ServiceProvider services = new ServiceCollection().AddLogging()
            .AddSingleton<ObjectPool<StringBuilder>>(new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy())).BuildServiceProvider();
        ScriptedClient client = new();
        ExtendedAgentOptions options = new()
        {
            ProviderType = provider,
            ModelId = "test",
            MaxOutputTokens = 256,
            MaxContextWindowTokens = 4096,
            MaximumIterationsPerRequest = 4,
        };
        HarnessAgent agent = options.CreateHarnessAgent(client, [tool], services);
        AgentSession session = await agent.CreateSessionAsync();
        List<AgentResponseUpdate> first = [];
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, "what time is it")], session))
        {
            first.Add(update);
        }
        ToolApprovalRequestContent request = first.SelectMany(update => update.Contents).OfType<ToolApprovalRequestContent>().Single();
        JsonSerializerOptions serializerOptions = new(AIJsonUtilities.DefaultOptions);
        JsonElement serialized = await agent.SerializeSessionAsync(session, serializerOptions);
        AgentSession restoredSession = await agent.DeserializeSessionAsync(serialized, serializerOptions);
        List<AgentResponseUpdate> second = [];
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync([new ChatMessage(ChatRole.User, [request.CreateResponse(approved)])], restoredSession))
        {
            second.Add(update);
        }
        Assert.IsTrue(second.SelectMany(update => update.Contents).OfType<TextContent>().Any(content => content.Text.Contains("done", StringComparison.Ordinal)));
        Assert.AreEqual(2, client.Requests);
    }

    private sealed class ScriptedClient : IChatClient
    {
        public int Requests { get; private set; }
        public bool ReasoningMode { get; init; }
        public bool TwoTools { get; init; }
        public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return serviceType.IsInstanceOfType(this) ? this : null;
        }

        public void Dispose() { }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            Requests++;
            LastMessages = messages.ToArray();
            ChatResponseUpdate update = new() { Role = ChatRole.Assistant, MessageId = "message-" + Requests, ResponseId = "response-" + Requests };
            if (ReasoningMode)
            {
                update.Contents.Add(new TextReasoningContent("reasoning") { ProtectedData = "opaque-reasoning" });
                update.Contents.Add(new TextContent("done"));
                update.FinishReason = ChatFinishReason.Stop;
            }
            else if (Requests == 1)
            {
                update.Contents.Add(new FunctionCallContent("call-1", "GetCurrentTime", new Dictionary<string, object?>()));
                update.FinishReason = ChatFinishReason.ToolCalls;
            }
            else
            {
                update.Contents.Add(new TextContent("done"));
                update.FinishReason = ChatFinishReason.Stop;
            }
            yield return update;
            if (TwoTools && Requests == 1)
            {
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant, MessageId = update.MessageId, ResponseId = update.ResponseId,
                    Contents = [new FunctionCallContent("call-2", "ShowSummary", new Dictionary<string, object?>())],
                    FinishReason = ChatFinishReason.ToolCalls,
                };
            }
        }
    }
}
