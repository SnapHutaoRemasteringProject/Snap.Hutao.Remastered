// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.AI;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Core.Text.Json;

internal static class AgentConversationJsonOptions
{
    public static JsonSerializerOptions Create()
    {
        JsonSerializerOptions options = new(AIJsonUtilities.DefaultOptions)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };
        options.TypeInfoResolverChain.Insert(0, new ConversationResolver());
        return options;
    }

    private sealed class ConversationResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            // Use the SDK's own context for internal JSON-included members,
            // including approval confirmation state inaccessible to our context.
            return type.Assembly == typeof(AIContent).Assembly
                ? null
                : ((IJsonTypeInfoResolver)AgentConversationJsonContext.Default).GetTypeInfo(type, options);
        }
    }
}
