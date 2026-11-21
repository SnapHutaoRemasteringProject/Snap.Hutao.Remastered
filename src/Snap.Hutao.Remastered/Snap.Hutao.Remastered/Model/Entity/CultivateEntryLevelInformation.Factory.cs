// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Model.Entity.Primitive;
using Snap.Hutao.Remastered.Service.Cultivation;

namespace Snap.Hutao.Remastered.Model.Entity;

public sealed partial class CultivateEntryLevelInformation
{
    public static CultivateEntryLevelInformation From(Guid entryId, CultivateType type, LevelInformation source)
    {
        return type switch
        {
            CultivateType.AvatarAndSkill => new()
            {
                EntryId = entryId,
                AvatarLevelFrom = source.AvatarLevelFrom,
                AvatarLevelTo = source.AvatarLevelTo,
                AvatarIsPromoting = source.AvatarIsPromoting,
                SkillALevelFrom = source.SkillALevelFrom,
                SkillALevelTo = source.SkillALevelTo,
                SkillELevelFrom = source.SkillELevelFrom,
                SkillELevelTo = source.SkillELevelTo,
                SkillQLevelFrom = source.SkillQLevelFrom,
                SkillQLevelTo = source.SkillQLevelTo,
            },
            CultivateType.Weapon => new()
            {
                EntryId = entryId,
                WeaponLevelFrom = source.WeaponLevelFrom,
                WeaponLevelTo = source.WeaponLevelTo,
                WeaponIsPromoting = source.WeaponIsPromoting,
            },
            _ => throw HutaoException.NotSupported($"不支持的养成类型{type}"),
        };
    }
}
