// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Snap.Hutao.Remastered.ViewModel.AI;

[Service(ServiceLifetime.Scoped)]
internal sealed class AgentConversationProfileController(ISettingsProvider<AgentSettings> settingsProvider)
{
    private readonly AgentSettings settings = settingsProvider.CurrentValue;

    public static bool CanCreateRequestOptions([NotNullWhen(true)] ModelProviderProfile? providerProfile, [NotNullWhen(true)] ModelProfile? modelProfile)
    {
        if (providerProfile is null)
        {
            return false;
        }

        if (modelProfile is null || string.IsNullOrWhiteSpace(modelProfile.ModelId))
        {
            return false;
        }

        if (!providerProfile.ModelProfiles.Contains(modelProfile))
        {
            return false;
        }

        return true;
    }

    public ExtendedAgentOptions? CreateRequestOptions(ModelProviderProfile? providerProfile, ModelProfile? modelProfile)
    {
        if (!CanCreateRequestOptions(providerProfile, modelProfile))
        {
            return null;
        }

        return ExtendedAgentOptions.Create(providerProfile, modelProfile, settings.AgentOptions);
    }

    public ModelProviderProfile? ResolveModelProviderProfile(Guid? providerProfileId)
    {
        // Callers should be transparent about model provider profile resolution.
        // So not like ResolveModelProfile, we do not accept ModelProviderProfiles for now.
        ObservableSettingsCollection<ModelProviderProfile, Guid> collection = settings.ModelProviderProfiles;

        if (!providerProfileId.HasValue)
        {
            return collection.CurrentItem;
        }

        return collection.FirstOrDefault(profile => profile.Id == providerProfileId.Value) ?? collection.CurrentItem;
    }

    public ModelProfile? ResolveModelProfile(ModelProviderProfile? providerProfile, Guid? modelProfileId)
    {
        if (providerProfile?.ModelProfiles is not { } collection)
        {
            return null;
        }

        if (!modelProfileId.HasValue)
        {
            return collection.CurrentItem;
        }

        return collection.FirstOrDefault(profile => profile.Id == modelProfileId.Value) ?? collection.CurrentItem;
    }
}
