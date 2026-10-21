// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Core.AI.Collections.ObjectModel;
using Snap.Hutao.Remastered.Core.AI.Diagnostics;
using Snap.Hutao.Remastered.Service.AI.Models;
using System.Linq;
using System.Threading;

namespace Snap.Hutao.Remastered.ViewModel.AI;

[Service(ServiceLifetime.Scoped)]
internal sealed class AgentConversationCollectionController(IServiceProvider serviceProvider)
    : IAgentConversationDeleteHandler, IDisposable
{
    private readonly AgentConversationPersistenceController persistenceController = serviceProvider.GetRequiredService<AgentConversationPersistenceController>();
    private readonly AgentConversationProfileController profileController = serviceProvider.GetRequiredService<AgentConversationProfileController>();
    private readonly AgentConversationViewModelFactory conversationFactory = serviceProvider.GetRequiredService<AgentConversationViewModelFactory>();

    private bool disposed;

    public AdvancedObservableCollection<AgentConversationViewModel> Conversations { get; } = [];

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        foreach (AgentConversationViewModel conversation in Conversations)
        {
            conversation.Dispose();
        }
    }

    public bool DeleteConversation(AgentConversationViewModel conversation)
    {
        if (!Conversations.Contains(conversation) || conversation.IsBusy)
        {
            return false;
        }

        SentryDiagnostics.AddBreadcrumb("Delete chat conversation", SentryBreadcrumbCategories.AIChat, SentryBreadcrumbTypes.UI);

        bool isCurrentConversation = ReferenceEquals(Conversations.CurrentItem, conversation);
        int oldIndex = Conversations.IndexOf(conversation);
        if (isCurrentConversation && Conversations.Count > 1)
        {
            int newIndex = Math.Clamp(oldIndex, 0, Conversations.Count - 2);
            if (newIndex < oldIndex)
            {
                Conversations.CurrentItem = Conversations[newIndex];
            }
            else
            {
                Conversations.CurrentItem = Conversations[newIndex + 1];
            }
        }

        persistenceController.DeleteConversation(conversation);
        Conversations.Remove(conversation);
        conversation.Dispose();

        // Ensure there is always a conversation
        if (Conversations.Count is 0)
        {
            AgentConversationViewModel newConversation = CreateConversationCore();
            Conversations.Add(newConversation);
            Conversations.CurrentItem = newConversation;
        }

        return true;
    }

    public void LoadConversations()
    {
        foreach (AgentConversation conversation in persistenceController.LoadConversations().OrderByDescending(static item => item.UpdatedAt))
        {
            AgentConversationViewModel viewModel = conversationFactory.Create(conversation, this);
            ApplyConversationProfile(viewModel, conversation.ModelProviderProfileId, conversation.ModelProfileId);
            Conversations.Add(viewModel);
        }

        if (Conversations.Count is 0)
        {
            Conversations.Add(CreateConversationCore());
        }

        Conversations.MoveCurrentToFirst();
    }

    public void SaveConversations()
    {
        foreach (AgentConversationViewModel conversation in Conversations)
        {
            persistenceController.SaveConversation(conversation);
        }
    }

    public AgentConversationViewModel CreateConversation()
    {
        AgentConversationViewModel conversation = CreateConversationCore();
        Conversations.Insert(0, conversation);
        Conversations.CurrentItem = conversation;
        persistenceController.SaveConversation(conversation);
        return conversation;
    }

    private AgentConversationViewModel CreateConversationCore()
    {
        AgentConversationViewModel conversation = conversationFactory.Create(this);
        ApplyConversationProfile(conversation, null, null);
        return conversation;
    }

    private void ApplyConversationProfile(AgentConversationViewModel conversation, Guid? providerProfileId, Guid? modelProfileId)
    {
        ModelProviderProfile? providerProfile = profileController.ResolveModelProviderProfile(providerProfileId);
        conversation.ModelProviderProfile = providerProfile;
        conversation.ModelProfile = profileController.ResolveModelProfile(providerProfile, modelProfileId);
    }
}
