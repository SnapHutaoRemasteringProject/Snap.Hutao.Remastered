// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Hutao.Remastered.Core.Text.Json;

/// <summary>
/// AOT-safe facade over <see cref="JsonSerializer"/>.
/// Every member resolves a <see cref="JsonTypeInfo{T}"/> from the supplied options first;
/// the generic reflection overloads are never used here, so call sites stay free of
/// IL2026/IL3050 while the actual metadata comes from the registered context.
/// </summary>
internal static class JsonSerialize
{
    private static readonly JsonSerializerOptions ReflectionFallbackOptions = CreateReflectionFallbackOptions();

    public static string Serialize<T>(T value, JsonSerializerOptions options)
    {
        return JsonSerializer.Serialize(value, GetTypeInfo<T>(options));
    }

    public static string Serialize<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        return JsonSerializer.Serialize(value, typeInfo);
    }

    public static byte[] SerializeToUtf8Bytes<T>(T value, JsonSerializerOptions options)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, GetTypeInfo<T>(options));
    }

    public static void Serialize<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, GetTypeInfo<T>(options));
    }

    public static ValueTask SerializeAsync<T>(Stream stream, T value, JsonSerializerOptions options, CancellationToken token = default)
    {
        return new(JsonSerializer.SerializeAsync(stream, value, GetTypeInfo<T>(options), token));
    }

    public static void Serialize<T>(Stream stream, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(stream, value, GetTypeInfo<T>(options));
    }

    public static T? Deserialize<T>(string json, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize(json, GetTypeInfo<T>(options));
    }

    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize(utf8Json, GetTypeInfo<T>(options));
    }

    public static T? Deserialize<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize(ref reader, GetTypeInfo<T>(options));
    }

    public static T? Deserialize<T>(JsonElement element, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize(element, GetTypeInfo<T>(options));
    }

    public static ValueTask<T?> DeserializeAsync<T>(Stream stream, JsonSerializerOptions options, CancellationToken token = default)
    {
        return JsonSerializer.DeserializeAsync<T>(stream, GetTypeInfo<T>(options), token);
    }

    public static T? Deserialize<T>(Stream stream, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize(stream, GetTypeInfo<T>(options));
    }

    public static JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(options);

        JsonTypeInfo? typeInfo = JsonSerializeContexts.TryGetTypeInfo(type, options)
            ?? TryGetTypeInfo(type, options)
            ?? ReflectionFallbackOptions.GetTypeInfo(type);

        return typeInfo ?? throw new NotSupportedException($"The JSON type '{type}' is not registered in the active JsonSerializerContext.");
    }

    public static JsonTypeInfo<T> GetTypeInfo<T>(JsonSerializerOptions options)
    {
        return GetTypeInfo(typeof(T), options) is JsonTypeInfo<T> typeInfo
            ? typeInfo
            : throw new NotSupportedException($"The JSON type '{typeof(T)}' is not registered in the active JsonSerializerContext.");
    }

    private static JsonTypeInfo? TryGetTypeInfo(Type type, JsonSerializerOptions options)
    {
        try
        {
            return options.TypeInfoResolver?.GetTypeInfo(type, options);
        }
        catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException or ArgumentException)
        {
            return null;
        }
    }

    private static JsonSerializerOptions CreateReflectionFallbackOptions()
    {
        JsonSerializerOptions options = JsonWireOptions.Create();
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        return options;
    }
}
