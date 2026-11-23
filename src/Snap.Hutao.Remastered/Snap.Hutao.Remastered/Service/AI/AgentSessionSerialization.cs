// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;

namespace Snap.Hutao.Remastered.Service.AI;

internal static class AgentSessionSerialization
{
    public static ValueTask<JsonElement> SerializeAsync(AIAgent agent, AgentSession session, CancellationToken cancellationToken = default)
    {
        RemoveUnsupportedCompactionState(session);
        return agent.SerializeSessionAsync(session, cancellationToken: cancellationToken);
    }

    public static async ValueTask<AgentSession> DeserializeAsync(AIAgent agent, JsonElement serialized, CancellationToken cancellationToken = default)
    {
        AgentSession session = await agent.DeserializeSessionAsync(serialized, cancellationToken: cancellationToken).ConfigureAwait(false);
        RemoveUnsupportedCompactionState(session);
        return session;
    }

    private static void RemoveUnsupportedCompactionState(AgentSession session)
    {
        if (!JsonSerializer.IsReflectionEnabledByDefault)
        {
            // Agent Framework 1.28 lacks generated metadata for the incremental
            // compaction index. It can rebuild the index from the saved chat history.
            session.StateBag.TryRemoveValue(nameof(ContextWindowCompactionStrategy));
        }
    }
}
