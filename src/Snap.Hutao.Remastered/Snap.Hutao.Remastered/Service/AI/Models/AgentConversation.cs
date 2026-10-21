// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Service.AI.Observables;
using System.Text.Json;

namespace Snap.Hutao.Remastered.Service.AI.Models;

internal sealed class AgentConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    public Guid? ModelProviderProfileId { get; set; }

    public Guid? ModelProfileId { get; set; }

    public string? AccountSnapshot { get; set; }

    public string InputText { get; set; } = string.Empty;

    public List<Microsoft.Extensions.AI.ChatMessage>? SeedMessages { get; set; }

    public AgentTurnCheckpoint? LastTurnCheckpoint { get; set; }

    public JsonElement? SerializedSessionState { get; set; }

    public List<ObservableToolApprovalRequestContent> ToolApprovalRequests { get; set; } = [];

    public ObservableToolApprovalRequestContent? ToolApprovalRequest { get; set; }

    public ObservableChatMessageCollection Messages { get; set; } = [];
}
