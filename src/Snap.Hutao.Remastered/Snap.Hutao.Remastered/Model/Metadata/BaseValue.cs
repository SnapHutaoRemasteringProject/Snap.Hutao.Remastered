// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
[ProtoInclude(100, typeof(Snap.Hutao.Remastered.Model.Metadata.Avatar.AvatarBaseValue))]
[ProtoInclude(101, typeof(Snap.Hutao.Remastered.Model.Metadata.Monster.MonsterBaseValue))]
public class BaseValue
{
    [ProtoMember(1)]
    public float HpBase { get; init; }

    [ProtoMember(2)]
    public float AttackBase { get; init; }

    [ProtoMember(3)]
    public float DefenseBase { get; init; }

    public virtual float GetValue(FightProperty fightProperty)
    {
        return fightProperty switch
        {
            FightProperty.FIGHT_PROP_BASE_HP => HpBase,
            FightProperty.FIGHT_PROP_BASE_ATTACK => AttackBase,
            FightProperty.FIGHT_PROP_BASE_DEFENSE => DefenseBase,
            _ => 0,
        };
    }
}
