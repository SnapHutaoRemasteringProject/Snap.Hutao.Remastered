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
        }
    }
}
