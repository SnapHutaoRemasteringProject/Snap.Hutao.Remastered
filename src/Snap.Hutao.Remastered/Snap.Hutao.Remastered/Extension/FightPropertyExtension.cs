// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Collections.Frozen;

namespace Snap.Hutao.Remastered.Extension;

public static class FightPropertyExtension
{
    // Based on the game's FightProperty definition, these properties represent
    // percentage-based values (e.g., crit rate, crit damage, HP%, ATK%, etc.)
    private static readonly FrozenSet<FightProperty> PercentProps = FrozenSet.ToFrozenSet((FightProperty[])[
        FightProperty.FIGHT_PROP_HP_PERCENT,
        FightProperty.FIGHT_PROP_ATTACK_PERCENT,
        FightProperty.FIGHT_PROP_DEFENSE_PERCENT,
        FightProperty.FIGHT_PROP_SPEED_PERCENT,
        FightProperty.FIGHT_PROP_CRITICAL,
        FightProperty.FIGHT_PROP_ANTI_CRITICAL,
        FightProperty.FIGHT_PROP_CRITICAL_HURT,
        FightProperty.FIGHT_PROP_CHARGE_EFFICIENCY,
        FightProperty.FIGHT_PROP_ADD_HURT,
        FightProperty.FIGHT_PROP_SUB_HURT,
        FightProperty.FIGHT_PROP_HEAL_ADD,
        FightProperty.FIGHT_PROP_HEALED_ADD,
        FightProperty.FIGHT_PROP_PHYSICAL_SUB_HURT,
        FightProperty.FIGHT_PROP_PHYSICAL_ADD_HURT,
        FightProperty.FIGHT_PROP_FIRE_ADD_HURT,
        FightProperty.FIGHT_PROP_ELEC_ADD_HURT,
        FightProperty.FIGHT_PROP_WATER_ADD_HURT,
        FightProperty.FIGHT_PROP_GRASS_ADD_HURT,
        FightProperty.FIGHT_PROP_WIND_ADD_HURT,
        FightProperty.FIGHT_PROP_ROCK_ADD_HURT,
        FightProperty.FIGHT_PROP_ICE_ADD_HURT,
        FightProperty.FIGHT_PROP_HIT_HEAD_ADD_HURT,
        FightProperty.FIGHT_PROP_FIRE_SUB_HURT,
        FightProperty.FIGHT_PROP_ELEC_SUB_HURT,
        FightProperty.FIGHT_PROP_WATER_SUB_HURT,
        FightProperty.FIGHT_PROP_GRASS_SUB_HURT,
        FightProperty.FIGHT_PROP_WIND_SUB_HURT,
        FightProperty.FIGHT_PROP_ROCK_SUB_HURT,
        FightProperty.FIGHT_PROP_ICE_SUB_HURT,
        FightProperty.FIGHT_PROP_SKILL_CD_MINUS_RATIO,
        FightProperty.FIGHT_PROP_SHIELD_COST_MINUS_RATIO,
    ]);

    public static bool IsFightPropPercent(this FightProperty prop)
    {
        return PercentProps.Contains(prop);
    }
}
