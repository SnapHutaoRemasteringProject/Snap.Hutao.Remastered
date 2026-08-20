// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Intrinsic.Format;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar;
using System.Collections.Immutable;
using System.Globalization;

namespace Snap.Hutao.Remastered.Service.AvatarInfo.Factory;

public static class ReliquaryScoreCalculator
{
    public static double Calculate(
        ImmutableArray<FightProperty> recommendedSubProperties,
        ImmutableArray<ReliquaryProperty> subProperties,
        EnergyType energyType,
        bool isCritEffective)
    {
        bool hasCritHurt = isCritEffective || recommendedSubProperties.Contains(FightProperty.FIGHT_PROP_CRITICAL_HURT);

        double totalScore = 0;

        foreach (ReliquaryProperty subProperty in subProperties)
        {
            double weight = GetWeight(subProperty.PropertyType, recommendedSubProperties, hasCritHurt, energyType, isCritEffective);
            if (weight <= 0)
            {
                continue;
            }

            double value = ParseValue(subProperty.PropertyType, subProperty.Value);
            totalScore += ScoreStat(subProperty.PropertyType, value, weight);
        }

        return totalScore;
    }

    public static double CalculateWithWeights(IEnumerable<(FightProperty Prop, float Value)> subStats, Func<FightProperty, double> getWeight)
    {
        double totalScore = 0;

        foreach ((FightProperty prop, float value) in subStats)
        {
            double weight = getWeight(prop);
            if (weight <= 0)
            {
                continue;
            }

            double normalizedValue = NormalizeStatValue(prop, value);
            totalScore += ScoreStat(prop, normalizedValue, weight);
        }

        return totalScore;
    }

    private static double NormalizeStatValue(FightProperty prop, double rawValue)
    {
        // Backpack stores percentage values as decimals (e.g., 0.031 = 3.1%),
        // but ScoreStat expects percentage numbers (e.g., 3.1).
        return prop.IsFightPropPercent() ? rawValue * 100.0 : rawValue;
    }

    private static double ScoreStat(FightProperty prop, double value, double weight)
    {
        return prop switch
        {
            FightProperty.FIGHT_PROP_CRITICAL => value * 2.0 * weight,
            FightProperty.FIGHT_PROP_CRITICAL_HURT => value * 1.0 * weight,
            FightProperty.FIGHT_PROP_ELEMENT_MASTERY => value * 0.33 * weight,
            FightProperty.FIGHT_PROP_CHARGE_EFFICIENCY => value * 1.1979 * weight,
            FightProperty.FIGHT_PROP_HP_PERCENT => value * 1.33 * weight,
            FightProperty.FIGHT_PROP_ATTACK_PERCENT => value * 1.33 * weight,
            FightProperty.FIGHT_PROP_DEFENSE_PERCENT => value * 1.06 * weight,
            FightProperty.FIGHT_PROP_ATTACK => value * 0.398 * 0.5 * weight,
            FightProperty.FIGHT_PROP_HP => value * 0.026 * 0.66 * weight,
            FightProperty.FIGHT_PROP_DEFENSE => value * 0.335 * 0.66 * weight,
            _ => 0,
        };
    }

    private static double GetWeight(
        FightProperty propertyType,
        ImmutableArray<FightProperty> recommendedSubProperties,
        bool hasCritHurt,
        EnergyType energyType,
        bool isCritEffective)
    {
        // 非心海角色双爆强制有效，避免米游社推荐副属性缺失双爆时评分失真
        if (isCritEffective && propertyType is FightProperty.FIGHT_PROP_CRITICAL or FightProperty.FIGHT_PROP_CRITICAL_HURT)
        {
            return 1.0;
        }

        bool isRecommended = recommendedSubProperties.Contains(propertyType);

        if (propertyType is FightProperty.FIGHT_PROP_CHARGE_EFFICIENCY && !isRecommended)
        {
            return GetChargeEfficiencyWeight(hasCritHurt, energyType);
        }

        if (!isRecommended)
        {
            return 0;
        }

        double weight = 1.0;

        if (propertyType is FightProperty.FIGHT_PROP_HP or FightProperty.FIGHT_PROP_ATTACK or FightProperty.FIGHT_PROP_DEFENSE)
        {
            weight *= 0.5;
        }

        return weight;
    }

    private static double GetChargeEfficiencyWeight(bool hasCritHurt, EnergyType energyType)
    {
        if (energyType is not EnergyType.SPECIAL_ENERGY_NONE)
        {
            return hasCritHurt ? 0 : 1.0;
        }

        return hasCritHurt ? 0.2 : 1.0;
    }

    private static double ParseValue(FightProperty propertyType, string value)
    {
        FormatMethod formatMethod = propertyType.GetFormatMethod();
        if (formatMethod is FormatMethod.Percent)
        {
            if (value.EndsWith('%'))
            {
                value = value[..^1];
            }

            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        return double.Parse(value, CultureInfo.InvariantCulture);
    }
}
