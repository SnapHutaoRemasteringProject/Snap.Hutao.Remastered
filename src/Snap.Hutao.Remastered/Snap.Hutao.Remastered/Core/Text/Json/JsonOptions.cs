// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Core.Text.Json;

public static class JsonOptions
{
    /// <summary>
    /// Compatibility default used by paths that have not been migrated yet. Generated
    /// contexts are consulted by <see cref="JsonSerialize"/> directly, not through this
    /// resolver, because a <see cref="JsonSerializerContext"/> freezes its options instance.
    /// </summary>
    public static readonly JsonSerializerOptions Default = CreateDefault();

    /// <summary>
    /// Reflection-only options for the runtime C# scripting window and third-party
    /// compatibility paths. This is intentionally not used by application JSON paths.
    /// </summary>
    public static JsonSerializerOptions CreateReflection()
    {
        JsonSerializerOptions options = JsonSerializeContexts.CreateOptions();
        options.TypeInfoResolver = CreateReflectionResolver();
        return options;
    }

    /// <summary>
    /// Creates an options instance whose generated context is created from that exact
    /// configuration, so formatting switches such as
    /// <see cref="JsonSerializerOptions.WriteIndented"/> are honored by the resolved
    /// <c>JsonTypeInfo</c> as well.
    /// </summary>
    public static JsonSerializerOptions Create(Action<JsonSerializerOptions>? configure = null)
    {
        JsonSerializerOptions options = JsonSerializeContexts.CreateOptions();
        configure?.Invoke(options);
        options.TypeInfoResolver = CreateReflectionResolver();
        return options;
    }

    public static JsonSerializerOptions CreateCompact()
    {
        return Create(static options => options.WriteIndented = false);
    }

    private static JsonSerializerOptions CreateDefault()
    {
        return Create();
    }

    private static IJsonTypeInfoResolver CreateReflectionResolver()
    {
        // The only reflection resolver left in the application. Native AOT never constructs
        // it; the scripting window is already outside the AOT product surface.
        return new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                JsonTypeInfoResolvers.ResolveEnumType,
            },
        };
    }
}
