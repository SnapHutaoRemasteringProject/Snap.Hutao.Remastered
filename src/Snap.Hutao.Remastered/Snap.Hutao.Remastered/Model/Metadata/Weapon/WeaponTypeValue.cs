// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Model.Metadata.Weapon;

[ProtoContract]
public sealed class WeaponTypeValue
{
    public WeaponTypeValue()
    {
    }

    public WeaponTypeValue(FightProperty type, GrowCurveType value, float initValue)
    {
        Type = type;
        Value = value;
        InitValue = initValue;
    }

    [ProtoMember(1)]
    public float InitValue { get; init; }

    [ProtoMember(2)]
    public FightProperty Type { get; init; }

    [ProtoMember(3)]
    public GrowCurveType Value { get; init; }
}
