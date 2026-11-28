// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Collections.Immutable;
using Snap.Hutao.Remastered.Core.Text.Json;

namespace Snap.Hutao.Remastered.Model.Metadata.Weapon;

[JsonConverter(typeof(WeaponTypeValueCollectionConverter))]
public sealed class WeaponTypeValueCollection : ICollection<WeaponTypeValue>
{
    private readonly List<WeaponTypeValue> items = [];
    private SortedDictionary<FightProperty, GrowCurveType>? typeValues;
    private SortedDictionary<FightProperty, float>? typeInitValues;
    private ImmutableArray<WeaponTypeValue> array = default;

    public WeaponTypeValueCollection()
    {
    }

    public WeaponTypeValueCollection(ImmutableArray<WeaponTypeValue> array)
    {
        items.AddRange(array);
        this.array = array;
    }

    public ImmutableArray<WeaponTypeValue> Array
    {
        get => array.IsDefault ? array = [.. items] : array;
    }

    public IReadOnlyDictionary<FightProperty, GrowCurveType> TypeValues
    {
        get => typeValues ??= BuildTypeValues();
    }

    public IReadOnlyDictionary<FightProperty, float> TypeInitValues
    {
        get => typeInitValues ??= BuildTypeInitValues();
    }

    public int Count
    {
        get => items.Count;
    }

    public bool IsReadOnly
    {
        get => false;
    }

    public void Add(WeaponTypeValue item)
    {
        items.Add(item);
        Invalidate();
    }

    public void Clear()
    {
        items.Clear();
        Invalidate();
    }

    public bool Contains(WeaponTypeValue item)
    {
        return items.Contains(item);
    }

    public void CopyTo(WeaponTypeValue[] target, int arrayIndex)
    {
        items.CopyTo(target, arrayIndex);
    }

    public bool Remove(WeaponTypeValue item)
    {
        if (!items.Remove(item))
        {
            return false;
        }

        Invalidate();
        return true;
    }

    public IEnumerator<WeaponTypeValue> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return items.GetEnumerator();
    }

    private void Invalidate()
    {
        array = default;
        typeValues = null;
        typeInitValues = null;
    }

    private SortedDictionary<FightProperty, GrowCurveType> BuildTypeValues()
    {
        SortedDictionary<FightProperty, GrowCurveType> result = [];
        foreach (ref readonly WeaponTypeValue entry in Array.AsSpan())
        {
            result.Add(entry.Type, entry.Value);
        }

        return result;
    }

    private SortedDictionary<FightProperty, float> BuildTypeInitValues()
    {
        SortedDictionary<FightProperty, float> result = [];
        foreach (ref readonly WeaponTypeValue entry in Array.AsSpan())
        {
            result.Add(entry.Type, entry.InitValue);
        }

        return result;
    }
}

[SuppressMessage("", "SA1402")]
internal sealed class WeaponTypeValueCollectionConverter : JsonConverter<WeaponTypeValueCollection>
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
