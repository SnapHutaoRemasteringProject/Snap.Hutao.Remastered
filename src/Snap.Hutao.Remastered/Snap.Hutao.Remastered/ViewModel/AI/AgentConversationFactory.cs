// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Service.AI.Models;

namespace Snap.Hutao.Remastered.ViewModel.AI;

internal static class AgentConversationFactory
{
    public static AgentConversation Create(AgentConversationViewModel conversation)
    {
        return new()
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            ModelProviderProfileId = conversation.ModelProviderProfile?.Id,
            ModelProfileId = conversation.ModelProfile?.Id,
            AccountSnapshot = conversation.AccountSnapshot,
            InputText = conversation.InputText,
            SeedMessages = conversation.Runtime.SeedMessages,
            LastTurnCheckpoint = conversation.LastTurnCheckpoint,
            SerializedSessionState = conversation.Runtime.SerializedSessionState?.Clone(),
            ToolApprovalRequest = conversation.ToolApprovalRequest,
            ToolApprovalRequests = [.. conversation.ToolApprovalRequests],
            Messages = new(conversation.Messages),
        };
    }
}
