// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Core.Text.Json;

public static class JsonOptions
{
    /// <summary>
    /// Generated contracts are preferred. Unregistered types retain their reflection
    /// compatibility path only when the runtime enables JSON reflection.
    /// </summary>
    public static readonly JsonSerializerOptions Default = CreateDefault();

    /// <summary>
    /// Explicit reflection-only options for managed compatibility callers.
    /// This is intentionally not used by native application JSON paths.
    /// </summary>
    public static JsonSerializerOptions CreateReflection()
    {
        JsonSerializerOptions options = JsonSerializeContexts.CreateOptions();
        options.TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
            ? CreateReflectionResolver()
            : throw new NotSupportedException("JSON reflection is disabled in this runtime.");
        return options;
    }

    /// <summary>
    /// Creates an options instance whose generated resolvers receive that exact
    /// configuration, so formatting switches such as
    /// <see cref="JsonSerializerOptions.WriteIndented"/> are honored by the resolved
    /// <c>JsonTypeInfo</c> as well.
    /// </summary>
    public static JsonSerializerOptions Create(Action<JsonSerializerOptions>? configure = null)
    {
        JsonSerializerOptions options = JsonSerializeContexts.CreateOptions();
        configure?.Invoke(options);
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            options.TypeInfoResolver = JsonTypeInfoResolver.Combine(options.TypeInfoResolver, CreateReflectionResolver());
        }

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
        // Reuse the runtime's resolver without changing its shared modifiers. The feature
        // switch removes this compatibility path from a reflection-disabled AOT build.
        if (!JsonSerializer.IsReflectionEnabledByDefault)
        {
            throw new NotSupportedException("JSON reflection is disabled in this runtime.");
        }

        IJsonTypeInfoResolver resolver = JsonSerializerOptions.Default.TypeInfoResolver
            ?? throw new NotSupportedException("The runtime JSON reflection resolver is unavailable.");
        return resolver.WithAddedModifier(JsonTypeInfoResolvers.ResolveEnumType);
    }
}
