// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Model.Metadata.Avatar;

[JsonConverter(typeof(ConverterFactory))]
internal sealed class IdLevelParametersCollection<TId, TLevel, TParameter>
    where TId : notnull
    where TLevel : notnull
{
    private readonly SortedDictionary<TId, ImmutableArray<TParameter>> idParameters = [];
    private readonly SortedDictionary<TLevel, ImmutableArray<TParameter>> levelParameters = [];

    public IdLevelParametersCollection(ImmutableArray<IdLevelParameters<TId, TLevel, TParameter>> entries)
    {
        Count = entries.Length;
        foreach (IdLevelParameters<TId, TLevel, TParameter> entry in entries)
        {
            idParameters.Add(entry.Id, entry.Parameters);
            levelParameters.Add(entry.Level, entry.Parameters);
        }
    }

    public int Count { get; }

    internal IReadOnlyDictionary<TId, ImmutableArray<TParameter>> IdParameters { get => idParameters; }

    internal IReadOnlyDictionary<TLevel, ImmutableArray<TParameter>> LevelParameters { get => levelParameters; }

    public ImmutableArray<TParameter> this[TId id]
    {
        get => idParameters[id];
    }

    public ImmutableArray<TParameter> this[TLevel level]
    {
        get => levelParameters[level];
    }
}

[SuppressMessage("", "SA1402")]
file sealed class ConverterFactory : JsonConverterFactory
{
    private static readonly Type ConverterType = typeof(Converter<,,>);

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IdLevelParametersCollection<,,>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return Activator.CreateInstance(ConverterType.MakeGenericType(typeToConvert.GetGenericArguments())) as JsonConverter;
    }
}

[SuppressMessage("", "SA1402")]
file sealed class Converter<TId, TLevel, TParameter> : JsonConverter<IdLevelParametersCollection<TId, TLevel, TParameter>>
    where TId : notnull
    where TLevel : notnull
{
    public override IdLevelParametersCollection<TId, TLevel, TParameter> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // NullReferenceException inside System.Text.Json
        return new(JsonSerializer.Deserialize<ImmutableArray<IdLevelParameters<TId, TLevel, TParameter>>>(ref reader, options));
    }

    public override void Write(Utf8JsonWriter writer, IdLevelParametersCollection<TId, TLevel, TParameter> value, JsonSerializerOptions options)
    {
        throw new JsonException();
    }
}