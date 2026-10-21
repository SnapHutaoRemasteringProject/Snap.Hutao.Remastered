// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Service.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using System.Collections.Generic;

namespace Snap.Hutao.Remastered.ViewModel.AI;

[Service(ServiceLifetime.Scoped)]
internal sealed class AgentConversationPersistenceController(IAgentConversationProvider conversationProvider)
{
    private readonly IAgentConversationProvider conversationProvider = conversationProvider;

    public IEnumerable<AgentConversation> LoadConversations()
    {
        return conversationProvider.LoadConversations();
    }

    public void SaveConversation(AgentConversationViewModel conversation)
    {
        conversationProvider.SaveConversation(AgentConversationFactory.Create(conversation));
    }

    public void DeleteConversation(AgentConversationViewModel conversation)
    {
        conversationProvider.DeleteConversation(conversation.Id);
    }
}
