// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Collections.Immutable;
using Snap.Hutao.Remastered.Core.Text.Json;

namespace Snap.Hutao.Remastered.Model.Metadata.Weapon;

[JsonConverter(typeof(Converter))]
public sealed class WeaponTypeValueCollection
{
    private readonly SortedDictionary<FightProperty, GrowCurveType> typeValues = [];
    private readonly SortedDictionary<FightProperty, float> typeInitValues = [];

    public WeaponTypeValueCollection(ImmutableArray<WeaponTypeValue> array)
    {
        Array = array;
        foreach (ref readonly WeaponTypeValue entry in array.AsSpan())
        {
            typeValues.Add(entry.Type, entry.Value);
            typeInitValues.Add(entry.Type, entry.InitValue);
        }
    }

    public ImmutableArray<WeaponTypeValue> Array { get; }

    public IReadOnlyDictionary<FightProperty, GrowCurveType> TypeValues { get => typeValues; }

    public IReadOnlyDictionary<FightProperty, float> TypeInitValues { get => typeInitValues; }
}

[SuppressMessage("", "SA1402")]
file sealed class Converter : JsonConverter<WeaponTypeValueCollection>
{
    public override WeaponTypeValueCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new(MetadataCollectionJson.Deserialize<ImmutableArray<WeaponTypeValue>>(ref reader, options));
    }

    public override void Write(Utf8JsonWriter writer, WeaponTypeValueCollection value, JsonSerializerOptions options)
    {
        throw new JsonException();
    }
}
