// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Service.AI.Models;
using Microsoft.Agents.AI;

namespace Snap.Hutao.Remastered.Service.AI;

internal interface IAgentService
{
    ValueTask<HarnessAgent> CreateAgentAsync(ExtendedAgentOptions options, CancellationToken cancellationToken = default);

    ValueTask RunStreamingAsync(AgentRunStreamingContext context, TaskScheduler taskScheduler, CancellationToken cancellationToken);
}
