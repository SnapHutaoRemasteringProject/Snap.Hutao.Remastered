// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Service.AI.Models;
using System.Threading;
using Microsoft.Agents.AI;
using System.Threading.Tasks;
using Sentry;

namespace Snap.Hutao.Remastered.Service.AI;

internal interface IAgentService
{
    ValueTask<HarnessAgent> CreateAgentAsync(ExtendedAgentOptions options, CancellationToken cancellationToken = default);

    ValueTask<SpanStatus> RunStreamingAsync(AgentRunStreamingContext context, TaskScheduler taskScheduler, CancellationToken cancellationToken);
}
