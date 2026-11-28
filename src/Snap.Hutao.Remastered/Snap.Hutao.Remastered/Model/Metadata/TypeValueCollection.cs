// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Model.Metadata;

[JsonConverter(typeof(TypeValueCollectionConverterFactory))]
public sealed class TypeValueCollection<TType, TValue> : ICollection<TypeValue<TType, TValue>>
    where TType : notnull
{
    private readonly List<TypeValue<TType, TValue>> items = [];
    private SortedDictionary<TType, TValue>? typeValues;
    private ImmutableArray<TypeValue<TType, TValue>> array = default;

    public TypeValueCollection()
    {
    }

    public TypeValueCollection(ImmutableArray<TypeValue<TType, TValue>> array)
    {
        items.AddRange(array);
        this.array = array;
    }

    public ImmutableArray<TypeValue<TType, TValue>> Array
    {
        get => array.IsDefault ? array = [.. items] : array;
    }

    public IReadOnlyDictionary<TType, TValue> TypeValues
    {
        get => typeValues ??= BuildTypeValues();
    }

    public int Count
    {
        get => items.Count;
    }

    public bool IsReadOnly
    {
        get => false;
    }

    public void Add(TypeValue<TType, TValue> item)
    {
        items.Add(item);
        Invalidate();
    }

    public void Clear()
    {
        items.Clear();
        Invalidate();
    }

    public bool Contains(TypeValue<TType, TValue> item)
    {
        return items.Contains(item);
    }

    public void CopyTo(TypeValue<TType, TValue>[] target, int arrayIndex)
    {
        items.CopyTo(target, arrayIndex);
    }

    public bool Remove(TypeValue<TType, TValue> item)
    {
        if (!items.Remove(item))
        {
            return false;
        }

        Invalidate();
        return true;
    }

    public IEnumerator<TypeValue<TType, TValue>> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return items.GetEnumerator();
    }

    public TValue? GetValueOrDefault(TType type)
    {
        return TypeValues.GetValueOrDefault(type);
    }

    private void Invalidate()
    {
        array = default;
        typeValues = null;
    }

    private SortedDictionary<TType, TValue> BuildTypeValues()
    {
        SortedDictionary<TType, TValue> result = [];
        foreach (ref readonly TypeValue<TType, TValue> entry in Array.AsSpan())
        {
            result.Add(entry.Type, entry.Value);
        }

        return result;
    }
}

[SuppressMessage("", "SA1402")]
internal sealed class TypeValueCollectionConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(TypeValueCollection<,>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(TypeValueCollection<FightProperty, float>)) return new Converter<FightProperty, float>();
        if (typeToConvert == typeof(TypeValueCollection<FightProperty, GrowCurveType>)) return new Converter<FightProperty, GrowCurveType>();
        if (typeToConvert == typeof(TypeValueCollection<GrowCurveType, float>)) return new Converter<GrowCurveType, float>();
        throw new NotSupportedException("Unregistered metadata collection type; add a closed converter before using it.");
    }
}

[SuppressMessage("", "SA1402")]
file sealed class Converter<TType, TValue> : JsonConverter<TypeValueCollection<TType, TValue>>
    where TType : notnull
{
    public override TypeValueCollection<TType, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return typeof(TType) == typeof(FightProperty) && (typeof(TValue) == typeof(float) || typeof(TValue) == typeof(GrowCurveType))
            || typeof(TType) == typeof(GrowCurveType) && typeof(TValue) == typeof(float)
            ? new(MetadataCollectionJson.Deserialize<ImmutableArray<TypeValue<TType, TValue>>>(ref reader, options))
            : new(JsonSerialize.Deserialize<ImmutableArray<TypeValue<TType, TValue>>>(ref reader, options));
    }

    public override void Write(Utf8JsonWriter writer, TypeValueCollection<TType, TValue> value, JsonSerializerOptions options)
    {
        throw new JsonException();
    }
}
