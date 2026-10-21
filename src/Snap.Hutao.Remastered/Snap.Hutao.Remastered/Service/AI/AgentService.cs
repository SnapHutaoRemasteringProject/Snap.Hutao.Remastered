// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Sentry;
using Snap.Hutao.Remastered.Core.AI.Diagnostics;
using Snap.Hutao.Remastered.Core.AI.Text.Json;
using Snap.Hutao.Remastered.Core.AI.Threading;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Observables;
using Snap.Hutao.Remastered.ViewModel.AI;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Hutao.Remastered.Service.AI;

[Service(ServiceLifetime.Scoped, typeof(IAgentService))]
internal sealed class AgentService(IServiceProvider serviceProvider) : IAgentService, IDisposable
{
    private readonly List<IChatClient> clients = [];
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly AgentChatClientFactory chatClientFactory = serviceProvider.GetRequiredService<AgentChatClientFactory>();
    private readonly JsonSerializerOptions functionContentJsonOptions = serviceProvider.GetRequiredKeyedService<JsonSerializerOptions>(JsonSerializerOptionsKey.AIFunctionContent);

    public ValueTask<HarnessAgent> CreateAgentAsync(ExtendedAgentOptions options, CancellationToken cancellationToken = default)
    {
        IChatClient chatClient = chatClientFactory.Create(options);
        clients.Add(chatClient);
        return ValueTask.FromResult(options.CreateHarnessAgent(chatClient, CreateBuiltInTools(), serviceProvider));
    }

    public void Dispose()
    {
        foreach (IChatClient client in clients) { client.Dispose(); }
        clients.Clear();
    }

    public async ValueTask<SpanStatus> RunStreamingAsync(AgentRunStreamingContext context, TaskScheduler taskScheduler, CancellationToken cancellationToken)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.AIChatStream, "Run streaming chat completion");
        span.SetTag(SentryTags.AIProvider, context.Options.ProviderType.ToString());
        span.SetTag(SentryTags.AIModel, context.Options.ModelId);

        try
        {
            await Task.Yield();
            IEnumerable<ChatMessage> messages = context.SeedMessages is { Count: > 0 } seed ? seed.Append(context.InputMessage) : [context.InputMessage];
            await foreach (AgentResponseUpdate update in context.Agent.RunStreamingAsync(messages, context.Session, context.Options.AsAgentRunOptions(), cancellationToken))
            {
                ExtendedAgentResponseUpdate pendingUpdate = ExtendedAgentResponseUpdate.Create(update, functionContentJsonOptions);
                if (pendingUpdate.IsEmpty)
                {
                    // Fast path for updates that do not contain any content. This avoids unnecessary dispatching to the UI thread.
                    continue;
                }

                await taskScheduler.Run(ProcessPendingUpdate, context, pendingUpdate, cancellationToken);
            }

            return SpanStatus.Ok;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            span.Finish(SpanStatus.Cancelled);
            throw;
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.AIChatStream);

            ObservableErrorContent errorContent = ObservableErrorContent.Create(ex.Message);
            await taskScheduler.Run(static (context, errorContent) =>
            {
                ObservableChatMessage responseMessage = context.EnsureTargetResponseMessage();
                responseMessage.Contents.AddOrUpdate(errorContent);
            }, context, errorContent, cancellationToken);

            return SpanStatus.InternalError;
        }
    }

    private static void ProcessPendingUpdate(AgentRunStreamingContext context, ExtendedAgentResponseUpdate pendingUpdate)
    {
        foreach (ObservableToolApprovalRequestContent toolApprovalRequest in pendingUpdate.ToolApprovalRequests)
        {
            // Certain updates may only contains a single tool approval request without any content.
            // For example, when the reasoning_effort is set to none.
            toolApprovalRequest.TargetMessageId = context.EnsureTargetResponseMessage().Id;
            if (!context.Conversation.ToolApprovalRequests.Any(request => request.RequestId == toolApprovalRequest.RequestId))
            {
                context.Conversation.ToolApprovalRequests.Add(toolApprovalRequest);
            }
            context.Conversation.ToolApprovalRequest = context.Conversation.ToolApprovalRequests.FirstOrDefault(static request => request.CanRespond);
        }

        if (pendingUpdate.ObservableContents.Count is 0)
        {
            return;
        }

        ObservableChatMessage responseMessage = context.EnsureTargetResponseMessage();
        foreach (ObservableAIContent observableContent in pendingUpdate.ObservableContents)
        {
            responseMessage.Contents.AddOrUpdate(observableContent);
        }
    }

    private static IList<AITool> CreateBuiltInTools()
    {
        return
        [
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(BuiltInFunctions.GetCurrentTime)),
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(BuiltInFunctions.ShowSummary))
        ];
    }
}
