// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.AI;

namespace Snap.Hutao.Remastered.Service.AI.Models;

internal sealed class AgentTurnCheckpoint
{
    public required ChatMessage InputMessage { get; set; }
    public string? TitleInput { get; set; }
    public int MessageCount { get; set; }
    public JsonElement? SerializedSessionState { get; set; }
    public List<ChatMessage>? SeedMessages { get; set; }
}
