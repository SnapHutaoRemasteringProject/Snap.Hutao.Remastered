// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Service.AI.Models;
using System.Collections.Generic;

namespace Snap.Hutao.Remastered.Service.AI;

internal interface IAgentConversationProvider
{
    IReadOnlyList<AgentConversation> LoadConversations();

    void SaveConversation(AgentConversation conversation);

    void DeleteConversation(Guid id);
}
