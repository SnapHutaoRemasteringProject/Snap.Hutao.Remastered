// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Model.Metadata;

[JsonConverter(typeof(ConverterFactory))]
public sealed class TypeValueCollection<TType, TValue>
    where TType : notnull
{
    private readonly SortedDictionary<TType, TValue> typeValues = [];

    public TypeValueCollection(ImmutableArray<TypeValue<TType, TValue>> array)
    {
        Array = array;
        foreach (ref readonly TypeValue<TType, TValue> entry in array.AsSpan())
        {
            typeValues.Add(entry.Type, entry.Value);
        }
    }

    public ImmutableArray<TypeValue<TType, TValue>> Array { get; }

    public IReadOnlyDictionary<TType, TValue> TypeValues { get => typeValues; }

    public TValue? GetValueOrDefault(TType type)
    {
        return typeValues.GetValueOrDefault(type);
    }
}

[SuppressMessage("", "SA1402")]
file sealed class ConverterFactory : JsonConverterFactory
{
    private static readonly Type ConverterType = typeof(Converter<,>);

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(TypeValueCollection<,>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(TypeValueCollection<FightProperty, float>)) return new Converter<FightProperty, float>();
        if (typeToConvert == typeof(TypeValueCollection<FightProperty, GrowCurveType>)) return new Converter<FightProperty, GrowCurveType>();
        if (typeToConvert == typeof(TypeValueCollection<GrowCurveType, float>)) return new Converter<GrowCurveType, float>();
        if (!RuntimeFeature.IsDynamicCodeSupported) throw new NotSupportedException("Unregistered Native AOT metadata collection type.");
        return Activator.CreateInstance(ConverterType.MakeGenericType(typeToConvert.GetGenericArguments())) as JsonConverter;
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
            : new(JsonSerializer.Deserialize<ImmutableArray<TypeValue<TType, TValue>>>(ref reader, options));
    }

    public override void Write(Utf8JsonWriter writer, TypeValueCollection<TType, TValue> value, JsonSerializerOptions options)
    {
        throw new JsonException();
    }
}
