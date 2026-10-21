// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Snap.Hutao.Remastered.Core.AI.Text.Json;
using Snap.Hutao.Remastered.Service.AI.Settings;
using Snap.Hutao.Remastered.ViewModel.AI;
using Snap.Hutao.Remastered.ViewModel.AI.Settings;
using System.Text;

namespace Snap.Hutao.Remastered.Service.AI;

internal static class AgentServiceCollectionExtensions
{
    public static IServiceCollection AddAccountInsightsAgent(this IServiceCollection services)
    {
        services.AddJsonSerializerOptions();
        services.TryAddSingleton<ObjectPool<StringBuilder>>(new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy()));
        services.AddSingleton<ISettingsProvider<AgentSettings>>(static provider => new JsonSettingsProvider<AgentSettings>("agent-settings", provider.GetRequiredKeyedService<JsonSerializerOptions>(JsonSerializerOptionsKey.Settings)));
        services.AddScoped<AgentAccountContext>();
        services.AddScoped<AgentChatClientFactory>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IModelProfileService, ModelProfileService>();
        services.AddScoped<IAgentConversationProvider, AgentConversationFileProvider>();
        services.AddScoped<AgentConversationPersistenceController>();
        services.AddScoped<AgentConversationProfileController>();
        services.AddScoped<AgentConversationRuntimeController>();
        services.AddScoped<AgentConversationTurnController>();
        services.AddScoped<AgentConversationViewModelFactory>();
        services.AddScoped<AgentConversationCollectionController>();
        services.AddScoped<SettingsModelConfigurationViewModel>();
        services.AddScoped<AgentViewModel>();
        return services;
    }
}
