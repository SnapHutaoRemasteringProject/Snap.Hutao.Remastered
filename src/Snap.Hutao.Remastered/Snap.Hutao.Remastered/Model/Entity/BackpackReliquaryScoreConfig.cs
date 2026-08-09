// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity.Abstraction;
using Snap.Hutao.Remastered.Model.Intrinsic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Remastered.Model.Entity;

[Table("backpack_reliquary_score_config")]
public sealed class BackpackReliquaryScoreConfig : IAppDbEntity
{
    [Key]
    public Guid InnerId { get; set; }

    public ReliquaryScoreConfigPreset PresetKey { get; set; } = ReliquaryScoreConfigPreset.Default;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public double CritWeight { get; set; } = 1.0;

    public double CritHurtWeight { get; set; } = 1.0;

    public double AttackPercentWeight { get; set; } = 0.2;

    public double ChargeEfficiencyWeight { get; set; } = 0.2;

    public double ElementalMasteryWeight { get; set; } = 0.2;

    public double HpPercentWeight { get; set; }

    public double DefensePercentWeight { get; set; }

    public double GetWeight(FightProperty prop)
    {
        return prop switch
        {
            FightProperty.FIGHT_PROP_CRITICAL => CritWeight,
            FightProperty.FIGHT_PROP_CRITICAL_HURT => CritHurtWeight,
            FightProperty.FIGHT_PROP_ATTACK_PERCENT => AttackPercentWeight,
            FightProperty.FIGHT_PROP_CHARGE_EFFICIENCY => ChargeEfficiencyWeight,
            FightProperty.FIGHT_PROP_ELEMENT_MASTERY => ElementalMasteryWeight,
            FightProperty.FIGHT_PROP_HP_PERCENT => HpPercentWeight,
            FightProperty.FIGHT_PROP_DEFENSE_PERCENT => DefensePercentWeight,
            FightProperty.FIGHT_PROP_HP or FightProperty.FIGHT_PROP_ATTACK or FightProperty.FIGHT_PROP_DEFENSE => 0,
            _ => 0,
        };
    }

    public BackpackReliquaryScoreConfig Clone()
    {
        return new()
        {
            InnerId = InnerId,
            PresetKey = PresetKey,
            Name = Name,
            IsActive = IsActive,
            CritWeight = CritWeight,
            CritHurtWeight = CritHurtWeight,
            AttackPercentWeight = AttackPercentWeight,
            ChargeEfficiencyWeight = ChargeEfficiencyWeight,
            ElementalMasteryWeight = ElementalMasteryWeight,
            HpPercentWeight = HpPercentWeight,
            DefensePercentWeight = DefensePercentWeight,
        };
    }
}
