// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Agents.AI;
using Snap.Hutao.Remastered.Core.AI.Threading;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Linq;
using Sentry;
using Snap.Hutao.Remastered.Core.AI.Diagnostics;
using Snap.Hutao.Remastered.Core.AI.Text.Json;
using Snap.Hutao.Remastered.Resource.Localization.AI;
using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Observables;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Hutao.Remastered.ViewModel.AI;

[Service(ServiceLifetime.Scoped)]
internal sealed class AgentConversationTurnController(IServiceProvider serviceProvider)
{
    private readonly IAgentService agentService = serviceProvider.GetRequiredService<IAgentService>();
    private readonly AgentAccountContext accountContext = serviceProvider.GetRequiredService<AgentAccountContext>();
    private readonly AgentSettings settings = serviceProvider.GetRequiredService<ISettingsProvider<AgentSettings>>().CurrentValue;
    private readonly AgentConversationProfileController profileController = serviceProvider.GetRequiredService<AgentConversationProfileController>();
    private readonly AgentConversationRuntimeController runtimeController = serviceProvider.GetRequiredService<AgentConversationRuntimeController>();
    private readonly AgentConversationPersistenceController persistenceController = serviceProvider.GetRequiredService<AgentConversationPersistenceController>();
    private readonly JsonSerializerOptions functionContentJsonOptions = serviceProvider.GetRequiredKeyedService<JsonSerializerOptions>(JsonSerializerOptionsKey.AIFunctionContent);

    public void SaveApprovalProgress(AgentConversationViewModel conversation) => persistenceController.SaveConversation(conversation);

    public string UserName { get => AgentOptionsNormalizer.NormalizeUserName(settings.AgentOptions.UserName); }

    public static bool CanRespondToToolApproval(AgentConversationViewModel conversation)
    {
        if (conversation.IsBusy)
        {
            return false;
        }

        if (conversation.ToolApprovalRequest?.CanRespond is not true)
        {
            return false;
        }

        return AgentConversationProfileController.CanCreateRequestOptions(conversation.ModelProviderProfile, conversation.ModelProfile);
    }

    public async Task SendMessageAsync(AgentConversationViewModel conversation, ChatMessage inputMessage, string? titleInput, CancellationToken cancellationToken)
    {
        ToolApprovalResponseContent? toolApprovalResponseContent = GetToolApprovalResponseContent(inputMessage);
        List<AgentConversationToolApprovalTurnState>? toolApprovalState = null;
        if (toolApprovalResponseContent is not null)
        {
            if (conversation.IsBusy) { return; }
            toolApprovalState = [];
            foreach (ToolApprovalResponseContent response in inputMessage.Contents.OfType<ToolApprovalResponseContent>())
            {
                ObservableToolApprovalRequestContent? request = conversation.ToolApprovalRequests.FirstOrDefault(pending => pending.RequestId == response.RequestId);
                if (request is null) { throw new InvalidOperationException("Unknown tool approval response."); }
                toolApprovalState.Add(new() { Request = request, ResponseContent = response });
            }
        }

        AgentConversationTurnOperation operation = new()
        {
            InputMessage = inputMessage,
            TitleInput = toolApprovalResponseContent is null ? titleInput : null,
            ToolApprovalState = toolApprovalState,
        };

        if (toolApprovalResponseContent is null)
        {
            conversation.LastTurnCheckpoint = new()
            {
                InputMessage = inputMessage.Clone(),
                TitleInput = titleInput,
                MessageCount = conversation.Messages.Count,
                SerializedSessionState = conversation.Runtime.SerializedSessionState?.Clone(),
                SeedMessages = conversation.Runtime.SeedMessages?.Select(static message => message.Clone()).ToList(),
            };
        }

        await RunTurnAsync(conversation, operation, cancellationToken);
    }

    public async Task RegenerateAsync(AgentConversationViewModel conversation, CancellationToken cancellationToken)
    {
        if (conversation.LastTurnCheckpoint is not { } checkpoint) { return; }
        conversation.Runtime.Session = null;
        conversation.Runtime.SerializedSessionState = checkpoint.SerializedSessionState?.Clone();
        conversation.Runtime.SeedMessages = checkpoint.SeedMessages?.Select(static message => message.Clone()).ToList();
        conversation.ToolApprovalRequest = null;
        conversation.ToolApprovalRequests.Clear();
        while (conversation.Messages.Count > checkpoint.MessageCount)
        {
            conversation.Messages.RemoveAt(conversation.Messages.Count - 1);
        }
        await RunTurnAsync(conversation, new() { InputMessage = checkpoint.InputMessage.Clone(), TitleInput = checkpoint.TitleInput }, cancellationToken);
    }

    public void AddExceptionMessages(AgentConversationViewModel conversation, Exception ex)
    {
        ObservableErrorContent errorContent = ObservableErrorContent.Create(ex.Message);
        ObservableChatMessage exceptionMessage = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now, content: errorContent);
        conversation.Messages.Add(exceptionMessage);

        conversation.UpdateConversationStatistics();
    }

    private static ObservableChatMessage? FindMessageById(ObservableChatMessageCollection messages, Guid messageId)
    {
        foreach (ObservableChatMessage message in messages)
        {
            if (message.Id.Equals(messageId))
            {
                return message;
            }
        }

        return null;
    }

    private static bool ShouldDisplayInputMessage(ChatMessage message)
    {
        return GetToolApprovalResponseContent(message) is null;
    }

    private static ToolApprovalResponseContent? GetToolApprovalResponseContent(ChatMessage message)
    {
        if (message.Contents.Count > 0 && message.Contents.All(static content => content is ToolApprovalResponseContent)
            && message.Contents[0] is ToolApprovalResponseContent responseContent)
        {
            return responseContent;
        }

        return null;
    }

    private static string CreateTitle(string input)
    {
        // TODO: Generate title based on first conversation content
        string title = input.ReplaceLineEndings(" ").Trim();
        const int maxLength = 40;
        if (title.Length <= maxLength)
        {
            return title;
        }

        return title[..maxLength] + "...";
    }

    private static ObservableChatMessage? ApplyToolApprovalState(AgentConversationViewModel conversation, List<AgentConversationToolApprovalTurnState>? states)
    {
        if (states is null)
        {
            return null;
        }

        ObservableChatMessage? targetResponseMessage = null;
        foreach (AgentConversationToolApprovalTurnState state in states)
        {
            ObservableToolApprovalRequestContent request = state.Request;
            ToolApprovalResponseContent responseContent = state.ResponseContent;

            request.IsHandled = true;
            request.Approved = responseContent.Approved;
            request.Reason = responseContent.Reason;

            targetResponseMessage ??= FindMessageById(conversation.Messages, request.TargetMessageId);
        }
        conversation.ToolApprovalRequests.Clear();
        conversation.ToolApprovalRequest = null;
        return targetResponseMessage;
    }

    private void AddInputMessage(AgentConversationViewModel conversation, ChatMessage inputMessage)
    {
        if (!ShouldDisplayInputMessage(inputMessage))
        {
            return;
        }

        ObservableChatMessage observableInputMessage = ObservableChatMessage.Create(inputMessage, functionContentJsonOptions);
        conversation.Messages.Add(observableInputMessage);
    }

    private async Task RunTurnAsync(AgentConversationViewModel conversation, AgentConversationTurnOperation operation, CancellationToken cancellationToken)
    {
        if (profileController.CreateRequestOptions(conversation.ModelProviderProfile, conversation.ModelProfile) is not { } requestOptions)
        {
            // TODO: Consider throw there.
            return;
        }

        conversation.AccountSnapshot ??= accountContext.Snapshot;
        requestOptions = requestOptions with { SystemPrompt = accountContext.CreateSystemPrompt(requestOptions.SystemPrompt, conversation.AccountSnapshot) };

        AddInputMessage(conversation, operation.InputMessage);

        if (string.IsNullOrWhiteSpace(requestOptions.ApiKey))
        {
            conversation.Runtime.Reset();

            SentryDiagnostics.AddBreadcrumb("Chat blocked by missing API key", SentryBreadcrumbCategories.AIChat, SentryBreadcrumbTypes.UI);
            throw new AgentConversationException(SHName.UIXamlPagesAgentPageMessageConfigureApiKey);
        }

        HarnessAgent agent = await runtimeController.EnsureConversationAgentAsync(conversation.Runtime, requestOptions, cancellationToken);
        AgentSession session = await runtimeController.EnsureConversationSessionAsync(conversation.Runtime, agent, cancellationToken);
        AgentRunStreamingContext streamingContext = new()
        {
            Conversation = conversation,
            InputMessage = operation.InputMessage,
            Agent = agent,
            Session = session,
            Options = requestOptions,
            SeedMessages = conversation.Runtime.SerializedSessionState is null ? conversation.Runtime.SeedMessages : null,
            TargetResponseMessage = ApplyToolApprovalState(conversation, operation.ToolApprovalState),
        };

        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.AIChatSend, "Run chat turn");
        span.SetTag(SentryTags.AIProvider, requestOptions.ProviderType.ToString());
        span.SetTag(SentryTags.AIModel, requestOptions.ModelId);

        conversation.IsBusy = true;

        try
        {
            TaskScheduler taskScheduler = AgentThreadingContext.Current.TaskScheduler;
            span.Finish(await agentService.RunStreamingAsync(streamingContext, taskScheduler, cancellationToken));

            conversation.UpdatedAt = DateTimeOffset.Now;
            if (operation.TitleInput is not null && string.IsNullOrWhiteSpace(conversation.Title))
            {
                conversation.Title = CreateTitle(operation.TitleInput);
            }

        }
        catch (OperationCanceledException)
        {
            span.Finish(SpanStatus.Cancelled);
        }
        catch (AgentConversationException)
        {
            span.Finish(SpanStatus.FailedPrecondition);
            throw;
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.AIChatSend);
            throw;
        }
        finally
        {
            try
            {
                await runtimeController.SerializeConversationSessionAsync(conversation.Runtime, streamingContext.Agent, streamingContext.Session, CancellationToken.None);
                conversation.Runtime.SeedMessages = null;
                persistenceController.SaveConversation(conversation);
            }
            finally
            {
                conversation.UpdateConversationStatistics();
                conversation.IsBusy = false;
            }
        }
    }

    private sealed class AgentConversationTurnOperation
    {
        public required ChatMessage InputMessage { get; init; }

        public string? TitleInput { get; init; }

        public List<AgentConversationToolApprovalTurnState>? ToolApprovalState { get; init; }
    }

    private sealed class AgentConversationToolApprovalTurnState
    {
        public required ObservableToolApprovalRequestContent Request { get; init; }

        public required ToolApprovalResponseContent ResponseContent { get; init; }
    }
}
