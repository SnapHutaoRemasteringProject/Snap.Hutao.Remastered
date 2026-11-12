// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Microsoft.Extensions.AI;
using Snap.Hutao.Remastered.Service.AI.Models;
using System.Collections.Concurrent;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Core.AI;

internal static class EnumBox
{
    public static EnumBox<T> Of<T>(T value)
        where T : struct, Enum
    {
        return EnumBox<T>.Of(value);
    }
}

[JsonConverter(typeof(EnumBoxJsonConverterFactory))]
internal sealed class EnumBox<T>
    where T : struct, Enum
{
    private static readonly ConcurrentDictionary<T, EnumBox<T>> Cache = [];

    private EnumBox(T value)
    {
        Value = value;
    }

    public T Value { get; }

    internal static EnumBox<T> Of(T value)
    {
        return Cache.GetOrAdd(value, static value => new(value));
    }
}

internal static class EnumBoxJsonConverters
{
    private static readonly Dictionary<Type, JsonConverter> Converters = [];

    static EnumBoxJsonConverters()
    {
        Register(new EnumBoxJsonConverter<ModelProviderType>());
        Register(new EnumBoxJsonConverter<ReasoningEffort>());
    }

    public static void Register<T>(JsonConverter<EnumBox<T>> converter)
        where T : struct, Enum
    {
        Converters[typeof(EnumBox<T>)] = converter;
    }

    public static JsonConverter Resolve(Type enumBoxType)
    {
        return Converters.TryGetValue(enumBoxType, out JsonConverter? converter)
            ? converter
            : throw new NotSupportedException($"The enum box JSON type '{enumBoxType}' must be registered in a generated JsonSerializerContext.");
    }
}

[SuppressMessage("", "SA1402")]
internal sealed class EnumBoxJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(EnumBox<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return EnumBoxJsonConverters.Resolve(typeToConvert);
    }
}

[SuppressMessage("", "SA1402")]
internal sealed class EnumBoxJsonConverter<T> : JsonConverter<EnumBox<T>>
    where T : struct, Enum
{
    public override EnumBox<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonTypeInfo<T> typeInfo = ResolveTypeInfo(options);
        return EnumBox.Of(JsonSerializer.Deserialize(ref reader, typeInfo));
    }

    public override void Write(Utf8JsonWriter writer, EnumBox<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, ResolveTypeInfo(options));
    }

    private static JsonTypeInfo<T> ResolveTypeInfo(JsonSerializerOptions options)
    {
        JsonTypeInfo? typeInfo = options.TypeInfoResolver?.GetTypeInfo(typeof(T), options);

        return typeInfo is JsonTypeInfo<T> typed
            ? typed
            : throw new NotSupportedException($"The enum JSON type '{typeof(T)}' is not registered in the active JsonSerializerContext.");
    }
}
