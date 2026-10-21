// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.ObjectPool;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Core.AI.ObjectPool;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Hutao.Remastered.Service.AI.Compatibility.OpenAIChatCompletion;

// Specially designed ChatHistoryProvider to make sure that:
// 1. 'reasoning_content'
// 2. 'content'
// Can be round-tripped correctly in the chat history
internal sealed class RoundTripInMemoryChatHistoryProvider(ObjectPool<StringBuilder> stringBuilderPool)
    : ChatHistoryProvider(null, null, StoreInputResponseMessageFilter)
{
    private const string StateName = "round_trip_chat_history";
    private readonly ObjectPool<StringBuilder> stringBuilderPool = stringBuilderPool;
    private readonly ProviderSessionState<State> sessionState = new((_ => new()), StateName, null);
    private IReadOnlyList<string>? stateKeys;

    public override IReadOnlyList<string> StateKeys { get => stateKeys ??= [sessionState.StateKey]; }

    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        State state = sessionState.GetOrInitializeState(context.Session);
        // Raw SDK messages are not serialized by AIJsonUtilities. Rebuild their wire
        // representation after restoring a session so reasoning_content is replayed.
        foreach (ChatMessage message in state.Messages)
        {
            if (message.Role != ChatRole.Assistant || message.RawRepresentation is not null)
            {
                continue;
            }
            OpenAI.Chat.ChatMessage? raw = OpenAI.Chat.MicrosoftExtensionsAIChatExtensions.AsOpenAIChatMessages([message]).FirstOrDefault();
            if (raw is not null)
            {
                WorkaroundReasoningContentRoundTrip(message, raw);
                WorkaroundContentRoundTrip(raw);
                message.RawRepresentation = raw;
            }
        }
        return ValueTask.FromResult<IEnumerable<ChatMessage>>(state.Messages);
    }

    protected override ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        State state = sessionState.GetOrInitializeState(context.Session);

        List<ChatMessage> responseMessages = context.ResponseMessages is null ? [] : [.. context.ResponseMessages];
        if (responseMessages.Count > 0)
        {
            IReadOnlyList<OpenAI.Chat.ChatMessage> rawRepresentations = [.. OpenAI.Chat.MicrosoftExtensionsAIChatExtensions.AsOpenAIChatMessages(responseMessages)];

            // Raw representations should be the same count as response messages
            Debug.Assert(responseMessages.Count == rawRepresentations.Count);

            for (int i = 0; i < responseMessages.Count; i++)
            {
                ChatMessage responseMessage = responseMessages[i];
                OpenAI.Chat.ChatMessage rawRepresentation = rawRepresentations[i];

                WorkaroundReasoningContentRoundTrip(responseMessage, rawRepresentation);
                WorkaroundContentRoundTrip(rawRepresentation);

                responseMessage.RawRepresentation = rawRepresentation;
            }
        }

        state.Messages.AddRange(context.RequestMessages.Concat(responseMessages));
        return ValueTask.CompletedTask;
    }

    private static IEnumerable<ChatMessage> StoreInputResponseMessageFilter(IEnumerable<ChatMessage> responses)
    {
        foreach (ChatMessage response in responses)
        {
            ChatMessage newResponse = response.Clone();
            newResponse.Contents = [];

            foreach (AIContent content in response.Contents)
            {
                if (content is TextContent text && string.IsNullOrEmpty(text.Text))
                {
                    continue;
                }

                newResponse.Contents.Add(content);
            }

            yield return newResponse;
        }
    }

    private void WorkaroundReasoningContentRoundTrip(ChatMessage responseMessage, OpenAI.Chat.ChatMessage rawRepresentation)
    {
        using ObjectPoolLease<StringBuilder> reasoningContentBuilderLease = stringBuilderPool.Rent();
        foreach (AIContent content in responseMessage.Contents)
        {
            if (content is TextReasoningContent reasoningContent)
            {
                reasoningContentBuilderLease.Value.Append(reasoningContent.Text);
            }
        }

        if (reasoningContentBuilderLease.Value.Length > 0)
        {
            rawRepresentation.Patch.Set("$.reasoning_content"u8, reasoningContentBuilderLease.Value.ToString());
        }
    }

    private void WorkaroundContentRoundTrip(OpenAI.Chat.ChatMessage rawRepresentation)
    {
        if (rawRepresentation is not OpenAI.Chat.AssistantChatMessage assistantChatMessage ||
            assistantChatMessage.Content.Count(static part => part.Kind is OpenAI.Chat.ChatMessageContentPartKind.Text) <= 1)
        {
            return;
        }

        List<OpenAI.Chat.ChatMessageContentPart> normalizedParts = [];

        using ObjectPoolLease<StringBuilder> contentBuilderLease = stringBuilderPool.Rent();
        for (int j = 0; j < assistantChatMessage.Content.Count; j++)
        {
            OpenAI.Chat.ChatMessageContentPart part = assistantChatMessage.Content[j];
            if (part.Kind is OpenAI.Chat.ChatMessageContentPartKind.Text)
            {
                contentBuilderLease.Value.Append(part.Text);
            }
            else
            {
                if (contentBuilderLease.Value.Length > 0)
                {
                    normalizedParts.Add(OpenAI.Chat.ChatMessageContentPart.CreateTextPart(contentBuilderLease.Value.ToStringAndClear()));
                }

                normalizedParts.Add(part);
            }
        }

        if (contentBuilderLease.Value.Length > 0)
        {
            normalizedParts.Add(OpenAI.Chat.ChatMessageContentPart.CreateTextPart(contentBuilderLease.Value.ToStringAndClear()));
        }

        assistantChatMessage.Content.Clear();
        foreach (OpenAI.Chat.ChatMessageContentPart part in normalizedParts)
        {
            assistantChatMessage.Content.Add(part);
        }
    }

    public sealed class State
    {
        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];
    }
}
