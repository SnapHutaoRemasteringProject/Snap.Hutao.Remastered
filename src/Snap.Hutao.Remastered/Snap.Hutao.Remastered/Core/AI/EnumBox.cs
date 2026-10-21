// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

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

file sealed class EnumBoxJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return true;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(typeof(EnumBoxJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
    }
}

file sealed class EnumBoxJsonConverter<T> : JsonConverter<EnumBox<T>>
    where T : struct, Enum
{
    public override EnumBox<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return EnumBox.Of(JsonSerializer.Deserialize<T>(ref reader, options));
    }

    public override void Write(Utf8JsonWriter writer, EnumBox<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }
}
