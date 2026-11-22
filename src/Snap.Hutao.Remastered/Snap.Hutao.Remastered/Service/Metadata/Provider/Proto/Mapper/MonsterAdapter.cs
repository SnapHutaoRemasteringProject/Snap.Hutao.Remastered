// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;
using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using Arkhe = Snap.Hutao.Remastered.Model.Intrinsic.Arkhe;
using MonsterBaseValue = Snap.Hutao.Remastered.Model.Metadata.Monster.MonsterBaseValue;
using MonsterType = Snap.Hutao.Remastered.Model.Intrinsic.MonsterType;
using ProtoArkhe = Snap.Hutao.Remastered.Metadata.Arkhe;
using ProtoMonster = Snap.Hutao.Remastered.Metadata.Monster;
using ProtoMonsterBaseValue = Snap.Hutao.Remastered.Metadata.MonsterBaseValue;
using ProtoMonsterType = Snap.Hutao.Remastered.Metadata.MonsterType;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class MonsterAdapter : ProtoCategoryAdapter<ProtoMonster, Model.Metadata.Monster.Monster>
{
    public MonsterAdapter()
        : base(MetadataCategories.Monster)
    {
    }

    public override IEnumerable<ProtoMonster> Enumerate(LanguageMetadata language)
    {
        return language.Monsters;
    }

    public override Model.Metadata.Monster.Monster Map(ProtoMonster message, MetadataConvert convert)
    {
        List<string>? affixes = message.Affixes.Count is 0 ? null : [.. message.Affixes.Select(convert.Text)];

        return new()
        {
            Id = (MonsterId)message.Id,
            DescribeId = (MonsterDescribeId)message.DescribeId,
            MonsterName = convert.OptionalText(message.MonsterName),
            Name = convert.OptionalText(message.Name),
            Title = convert.OptionalText(message.Title),
            Description = convert.OptionalText(message.Description),
            Icon = convert.Text(message.Icon),
            Type = convert.ToEnum<ProtoMonsterType, MonsterType>(message.Type, nameof(message.Type)),
            Arkhe = convert.ToEnum<ProtoArkhe, Arkhe>(message.Arkhe, nameof(message.Arkhe)),
            Affixes = affixes,
            Drops = message.Drops.Count is 0 ? default(ImmutableArray<MaterialId>) : [.. message.Drops.Select(id => (MaterialId)id)],
            BaseValue = MapBaseValue(message.BaseValue, convert),
            GrowCurves = message.GrowCurves.Count is 0
                ? null
                : convert.FightGrowCurveCollection(
                    message.GrowCurves.Select(curve => (curve.Type, curve.Value)),
                    nameof(message.GrowCurves)),
        };
    }

    private static MonsterBaseValue? MapBaseValue(ProtoMonsterBaseValue? baseValue, MetadataConvert convert)
    {
        if (baseValue is null)
        {
            return null;
        }

        return new()
        {
            HpBase = convert.Single(baseValue.HpBase),
            AttackBase = convert.Single(baseValue.AttackBase),
            DefenseBase = convert.Single(baseValue.DefenseBase),
            FireSubHurt = convert.Single(baseValue.FireSubHurt),
            GrassSubHurt = convert.Single(baseValue.GrassSubHurt),
            WaterSubHurt = convert.Single(baseValue.WaterSubHurt),
            ElecSubHurt = convert.Single(baseValue.ElecSubHurt),
            WindSubHurt = convert.Single(baseValue.WindSubHurt),
            IceSubHurt = convert.Single(baseValue.IceSubHurt),
            RockSubHurt = convert.Single(baseValue.RockSubHurt),
            PhysicalSubHurt = convert.Single(baseValue.PhysicalSubHurt),
        };
    }
}
