// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[JsonConverter(typeof(IdLevelParametersCollectionConverterFactory))]
public sealed class IdLevelParametersCollection<TId, TLevel, TParameter> : ICollection<IdLevelParameters<TId, TLevel, TParameter>>
    where TId : notnull
    where TLevel : notnull
{
    private readonly List<IdLevelParameters<TId, TLevel, TParameter>> items = [];
    private SortedDictionary<TId, ImmutableArray<TParameter>>? idParameters;
    private SortedDictionary<TLevel, ImmutableArray<TParameter>>? levelParameters;

    public IdLevelParametersCollection()
    {
    }

    public IdLevelParametersCollection(ImmutableArray<IdLevelParameters<TId, TLevel, TParameter>> entries)
    {
        items.AddRange(entries);
    }

    public int Count
    {
        get => items.Count;
    }

    public bool IsReadOnly
    {
        get => false;
    }

    public IReadOnlyDictionary<TId, ImmutableArray<TParameter>> IdParameters
    {
        get => idParameters ??= BuildIdParameters();
    }

    public IReadOnlyDictionary<TLevel, ImmutableArray<TParameter>> LevelParameters
    {
        get => levelParameters ??= BuildLevelParameters();
    }

    public ImmutableArray<TParameter> this[TId id]
    {
        get => IdParameters[id];
    }

    public ImmutableArray<TParameter> this[TLevel level]
    {
        get => LevelParameters[level];
    }

    public void Add(IdLevelParameters<TId, TLevel, TParameter> item)
    {
        items.Add(item);
        Invalidate();
    }

    public void Clear()
    {
        items.Clear();
        Invalidate();
    }

    public bool Contains(IdLevelParameters<TId, TLevel, TParameter> item)
    {
        return items.Contains(item);
    }

    public void CopyTo(IdLevelParameters<TId, TLevel, TParameter>[] target, int arrayIndex)
    {
        items.CopyTo(target, arrayIndex);
    }

    public bool Remove(IdLevelParameters<TId, TLevel, TParameter> item)
    {
        if (!items.Remove(item))
        {
            return false;
        }

        Invalidate();
        return true;
    }

    public IEnumerator<IdLevelParameters<TId, TLevel, TParameter>> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return items.GetEnumerator();
    }

    private void Invalidate()
    {
        idParameters = null;
        levelParameters = null;
    }

    private SortedDictionary<TId, ImmutableArray<TParameter>> BuildIdParameters()
    {
        SortedDictionary<TId, ImmutableArray<TParameter>> result = [];
        foreach (IdLevelParameters<TId, TLevel, TParameter> entry in items)
        {
            result.Add(entry.Id, entry.Parameters);
        }

        return result;
    }

    private SortedDictionary<TLevel, ImmutableArray<TParameter>> BuildLevelParameters()
    {
        SortedDictionary<TLevel, ImmutableArray<TParameter>> result = [];
        foreach (IdLevelParameters<TId, TLevel, TParameter> entry in items)
        {
            result.Add(entry.Level, entry.Parameters);
        }

        return result;
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
