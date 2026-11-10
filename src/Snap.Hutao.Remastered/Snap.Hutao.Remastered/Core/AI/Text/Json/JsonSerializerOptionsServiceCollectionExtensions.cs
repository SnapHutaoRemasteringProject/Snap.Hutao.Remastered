// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snap.Hutao.Remastered.Core.Text.Json;
using System.Text.Encodings.Web;

namespace Snap.Hutao.Remastered.Core.AI.Text.Json;

internal static class JsonSerializerOptionsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddJsonSerializerOptions()
        {
            services.TryAddKeyedSingleton(JsonSerializerOptionsKey.Settings, CreateSettingsOptions());
            services.TryAddKeyedSingleton(JsonSerializerOptionsKey.AgentConversation, CreateAgentConversationOptions());
            services.TryAddKeyedSingleton(JsonSerializerOptionsKey.AIFunctionContent, CreateAIFunctionContentOptions());

            return services;
        }
    }

    private static JsonSerializerOptions CreateSettingsOptions()
    {
        return new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            TypeInfoResolver = AgentSettingsJsonContext.Default,
        };
    }

    private static JsonSerializerOptions CreateAgentConversationOptions()
    {
        return new(AIJsonUtilities.DefaultOptions)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };
    }

    private static JsonSerializerOptions CreateAIFunctionContentOptions()
    {
        return new(AIJsonUtilities.DefaultOptions);
    }
}
