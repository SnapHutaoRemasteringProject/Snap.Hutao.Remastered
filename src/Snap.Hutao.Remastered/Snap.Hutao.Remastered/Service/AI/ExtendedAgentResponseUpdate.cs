// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Snap.Hutao.Remastered.Service.AI.Observables;
using System.Collections.Generic;
using System.Text.Json;

namespace Snap.Hutao.Remastered.Service.AI;

internal sealed class ExtendedAgentResponseUpdate
{
    public List<ObservableAIContent> ObservableContents { get; } = [];

    public List<ObservableToolApprovalRequestContent> ToolApprovalRequests { get; } = [];

    public bool IsEmpty { get => ObservableContents.Count <= 0 && ToolApprovalRequests.Count == 0; }

    public static ExtendedAgentResponseUpdate Create(AgentResponseUpdate update, JsonSerializerOptions jsonOptions)
    {
        ExtendedAgentResponseUpdate extendedUpdate = new();
        foreach (AIContent content in update.Contents)
        {
            if (ObservableAIContent.Create(content, jsonOptions) is not { } observableContent)
            {
                continue;
            }

            if (observableContent is ObservableToolApprovalRequestContent request)
            {
                extendedUpdate.ToolApprovalRequests.Add(request);
            }
            else
            {
                extendedUpdate.ObservableContents.Add(observableContent);
            }
        }

        return extendedUpdate;
    }
}
