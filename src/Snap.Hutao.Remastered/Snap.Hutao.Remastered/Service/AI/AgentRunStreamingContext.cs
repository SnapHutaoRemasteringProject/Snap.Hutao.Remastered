// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Observables;
using Snap.Hutao.Remastered.ViewModel.AI;

namespace Snap.Hutao.Remastered.Service.AI;

internal sealed class AgentRunStreamingContext
{
    public required AgentConversationViewModel Conversation { get; init; }

    public required ChatMessage InputMessage { get; init; }

    public IReadOnlyList<ChatMessage>? SeedMessages { get; init; }

    public required HarnessAgent Agent { get; init; }

    public required AgentSession Session { get; init; }

    public required ExtendedAgentOptions Options { get; init; }

    public ObservableChatMessage? TargetResponseMessage { get; set; }

    // This method has to be called on the main thread
    public ObservableChatMessage EnsureTargetResponseMessage()
    {
        if (TargetResponseMessage is not { } responseMessage)
        {
            responseMessage = ObservableChatMessage.Create(ChatRole.Assistant, DateTimeOffset.Now, Options.ModelId);
            Conversation.Messages.Add(responseMessage);
            TargetResponseMessage = responseMessage;
        }

        return responseMessage;
    }
}
