// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Agents.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using System.Text.Json;

namespace Snap.Hutao.Remastered.ViewModel.AI;

internal sealed class AgentConversationRuntime
{
    public HarnessAgent? Agent { get; private set; }

    public ExtendedAgentOptions? AgentOptions { get; private set; }

    public AgentSession? Session { get; set; }

    public JsonElement? SerializedSessionState { get; set; }

    public List<Microsoft.Extensions.AI.ChatMessage>? SeedMessages { get; set; }

    public void Reset()
    {
        Agent = null;
        AgentOptions = null;
        Session = null;
    }

    public void Reset(HarnessAgent agent, ExtendedAgentOptions agentOptions)
    {
        Agent = agent;
        AgentOptions = agentOptions;
        Session = null;
    }
}
