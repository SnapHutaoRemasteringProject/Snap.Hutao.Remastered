// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[JsonConverter(typeof(IdLevelParametersCollectionConverterFactory))]
public sealed class IdLevelParametersCollection<TId, TLevel, TParameter>
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

    public IReadOnlyDictionary<TId, ImmutableArray<TParameter>> IdParameters { get => idParameters; }

    public IReadOnlyDictionary<TLevel, ImmutableArray<TParameter>> LevelParameters { get => levelParameters; }

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
internal sealed class IdLevelParametersCollectionConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IdLevelParametersCollection<,,>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(IdLevelParametersCollection<ProudSkillId, SkillLevel, float>)) return new Converter<ProudSkillId, SkillLevel, float>();
        throw new NotSupportedException("Unregistered metadata parameter type; add a closed converter before using it.");
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
        return typeof(TId) == typeof(ProudSkillId) && typeof(TLevel) == typeof(SkillLevel) && typeof(TParameter) == typeof(float)
            ? new(MetadataCollectionJson.Deserialize<ImmutableArray<IdLevelParameters<TId, TLevel, TParameter>>>(ref reader, options))
            : new(JsonSerialize.Deserialize<ImmutableArray<IdLevelParameters<TId, TLevel, TParameter>>>(ref reader, options));
    }

    public override void Write(Utf8JsonWriter writer, IdLevelParametersCollection<TId, TLevel, TParameter> value, JsonSerializerOptions options)
    {
        throw new JsonException();
    }
}
