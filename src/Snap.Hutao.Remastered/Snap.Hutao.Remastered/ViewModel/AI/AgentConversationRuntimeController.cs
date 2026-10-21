// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Agents.AI;
using Snap.Hutao.Remastered.Core.AI.Text.Json;
using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Hutao.Remastered.ViewModel.AI;

[Service(ServiceLifetime.Scoped)]
internal sealed class AgentConversationRuntimeController(IServiceProvider serviceProvider)
{
    private readonly IAgentService agentService = serviceProvider.GetRequiredService<IAgentService>();
    private readonly JsonSerializerOptions jsonOptions = serviceProvider.GetRequiredKeyedService<JsonSerializerOptions>(JsonSerializerOptionsKey.AgentConversation);

    public async ValueTask<HarnessAgent> EnsureConversationAgentAsync(AgentConversationRuntime runtime, ExtendedAgentOptions requestOptions, CancellationToken cancellationToken)
    {
        if (runtime.Agent is not null && requestOptions.AgentEquals(runtime.AgentOptions))
        {
            return runtime.Agent;
        }

        HarnessAgent agent = await agentService.CreateAgentAsync(requestOptions, cancellationToken);
        runtime.Reset(agent, requestOptions);
        return agent;
    }

    public async ValueTask<AgentSession> EnsureConversationSessionAsync(AgentConversationRuntime runtime, HarnessAgent agent, CancellationToken cancellationToken)
    {
        if (runtime.Session is not null)
        {
            return runtime.Session;
        }

        if (runtime.SerializedSessionState is JsonElement serializedState)
        {
            runtime.Session = await agent.DeserializeSessionAsync(serializedState, jsonOptions, cancellationToken);
        }
        else
        {
            runtime.Session = await agent.CreateSessionAsync(cancellationToken);
        }

        return runtime.Session;
    }

    public async ValueTask SerializeConversationSessionAsync(AgentConversationRuntime runtime, HarnessAgent agent, AgentSession session, CancellationToken cancellationToken)
    {
        JsonElement serializedState = await agent.SerializeSessionAsync(session, jsonOptions, cancellationToken);
        runtime.SerializedSessionState = serializedState.Clone();
    }
}
