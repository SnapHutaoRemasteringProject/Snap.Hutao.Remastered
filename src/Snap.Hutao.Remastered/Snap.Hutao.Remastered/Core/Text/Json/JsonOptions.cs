// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Core.Text.Json;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = CreateDefault();

    private static JsonSerializerOptions CreateDefault()
    {
        JsonSerializerOptions options = JsonWireOptions.Create();
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                JsonTypeInfoResolvers.ResolveEnumType,
            },
        };
        return options;
    }
}
