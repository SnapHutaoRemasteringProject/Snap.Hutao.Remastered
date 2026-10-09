// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar;
using System.Collections.Frozen;
using System.Collections.Immutable;
using MetadataSkill = Snap.Hutao.Remastered.Model.Metadata.Avatar.Skill;

namespace Snap.Hutao.Remastered.Service.AvatarInfo;

// 米游社「角色详情」返回的天赋等级包含了命座与固有天赋的加成，
// 需要减去加成才是可用于养成计算的 1 - 10 级基础等级。
public static class AvatarSkillExtraLevelResolver
{
    // 达达利亚固有天赋：普通攻击等级 +1
    private const uint TartagliaInherentGroupId = 3323U;

    public static FrozenDictionary<SkillId, SkillLevel> Resolve(SkillDepot depot, ImmutableArray<Constellation> constellations)
    {
        Dictionary<SkillId, SkillLevel> extraLevels = [];

        if (depot.Inherents is [_, _, { } inherent] && inherent.GroupId == TartagliaInherentGroupId)
        {
            extraLevels[depot.CompositeSkillsNoInherents[0].Id] = 1;
        }

        if (constellations.IsDefaultOrEmpty)
        {
            return extraLevels.ToFrozenDictionary();
        }

        // 命座按顺序激活，若当前命座未激活，则其后的命座一定也未激活
        foreach ((MetadataSkill metaConstellation, Constellation dataConstellation) in depot.Talents.Zip(constellations))
        {
            if (!dataConstellation.IsActived)
            {
                break;
            }

            if (metaConstellation.ExtraLevel is not { } extraLevel)
            {
                continue;
            }

            int index = extraLevel.Index switch
            {
                ExtraLevelIndexKind.NormalAttack => 0,
                ExtraLevelIndexKind.ElementalSkill => 1,
                ExtraLevelIndexKind.ElementalBurst => 2,
                _ => throw HutaoException.NotSupported("Unexpected extra level index."),
            };

            extraLevels[depot.CompositeSkillsNoInherents[index].Id] = extraLevel.Level;
        }

        return extraLevels.ToFrozenDictionary();
    }
}
