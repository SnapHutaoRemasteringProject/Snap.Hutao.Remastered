// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;
using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Metadata.Tower;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoTowerLevel = Snap.Hutao.Remastered.Metadata.TowerLevel;
using ProtoTowerWave = Snap.Hutao.Remastered.Metadata.TowerWave;
using ProtoTowerWaveMonster = Snap.Hutao.Remastered.Metadata.TowerWaveMonster;
using ProtoNameDescription = Snap.Hutao.Remastered.Metadata.NameDescription;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class TowerLevelAdapter : ProtoCategoryAdapter<ProtoTowerLevel, Model.Metadata.Tower.TowerLevel>
{
    public TowerLevelAdapter()
        : base(MetadataCategories.TowerLevel)
    {
    }

    public override IEnumerable<ProtoTowerLevel> Enumerate(LanguageMetadata language)
    {
        return language.TowerLevels;
    }

    public override Model.Metadata.Tower.TowerLevel Map(ProtoTowerLevel message, MetadataConvert convert)
    {
        return new()
        {
            Id = (TowerLevelId)message.Id,
            GroupId = (TowerLevelGroupId)message.GroupId,
            Index = message.Index,
            MonsterLevel = message.MonsterLevel,
            FirstMonsters = message.FirstMonsters.Count is 0 ? default(ImmutableArray<MonsterDescribeId>) : [.. message.FirstMonsters.Select(id => (MonsterDescribeId)id)],
            FirstWaves = [.. message.FirstWaves.Select(wave => MapWave(wave, convert))],
            FirstGadget = MapGadget(message.FirstGadget, convert),
            SecondMonsters = message.SecondMonsters.Count is 0 ? default(ImmutableArray<MonsterDescribeId>) : [.. message.SecondMonsters.Select(id => (MonsterDescribeId)id)],
            SecondWaves = [.. message.SecondWaves.Select(wave => MapWave(wave, convert))],
            SecondGadget = MapGadget(message.SecondGadget, convert),
        };
    }

    private static TowerWave MapWave(ProtoTowerWave wave, MetadataConvert convert)
    {
        return new()
        {
            Type = convert.RawEnum<WaveType>(wave.Type, nameof(wave.Type)),
            Description = convert.OptionalText(wave.Description),
            Monsters = [.. wave.Monsters.Select(monster => MapMonster(monster, convert))],
        };
    }

    private static TowerMonster MapMonster(ProtoTowerWaveMonster monster, MetadataConvert convert)
    {
        return new()
        {
            Id = (MonsterDescribeId)monster.Id,
            Count = monster.Count,
            AttackMonolith = monster.AttackMonolith,
            Affixes = monster.Affixes.Count is 0 ? default(ImmutableArray<NameDescription>) : [.. monster.Affixes.Select(affix => MapNameDescription(affix, convert))],
        };
    }

    private static NameDescription? MapGadget(ProtoNameDescription? gadget, MetadataConvert convert)
    {
        return gadget is null ? null : MapNameDescription(gadget, convert);
    }

    private static NameDescription MapNameDescription(ProtoNameDescription value, MetadataConvert convert)
    {
        return new(convert.Text(value.Name), convert.Text(value.Description));
    }
}
