// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Monster;

[ProtoContract]
public sealed class MonsterBaseValue : BaseValue
{
    [ProtoMember(4)]
    public float FireSubHurt { get; init; }

    [ProtoMember(5)]
    public float GrassSubHurt { get; init; }

    [ProtoMember(6)]
    public float WaterSubHurt { get; init; }

    [ProtoMember(7)]
    public float ElecSubHurt { get; init; }

    [ProtoMember(8)]
    public float WindSubHurt { get; init; }

    [ProtoMember(9)]
    public float IceSubHurt { get; init; }

    [ProtoMember(10)]
    public float RockSubHurt { get; init; }

    [ProtoMember(11)]
    public float PhysicalSubHurt { get; init; }

    public ImmutableArray<NameStringValue> SubHurts
    {
        get
        {
            return !field.IsDefault ? field : field =
            [
                FightPropertyFormat.ToNameStringValue(FightProperty.FIGHT_PROP_FIRE_SUB_HURT, FireSubHurt),
                FightPropertyFormat.ToNameStringValue(FightProperty.FIGHT_PROP_WATER_SUB_HURT, WaterSubHurt),
                FightPropertyFormat.ToNameStringValue(FightProperty.FIGHT_PROP_GRASS_SUB_HURT, GrassSubHurt),
                FightPropertyFormat.ToNameStringValue(FightProperty.FIGHT_PROP_ELEC_SUB_HURT, ElecSubHurt),
                FightPropertyFormat.ToNameStringValue(FightProperty.FIGHT_PROP_WIND_SUB_HURT, WindSubHurt),
                FightPropertyFormat.ToNameStringValue(FightProperty.FIGHT_PROP_ICE_SUB_HURT, IceSubHurt),
                FightPropertyFormat.ToNameStringValue(FightProperty.FIGHT_PROP_ROCK_SUB_HURT, RockSubHurt),
                FightPropertyFormat.ToNameStringValue(FightProperty.FIGHT_PROP_PHYSICAL_SUB_HURT, PhysicalSubHurt),
            ];
        }
    }
}
