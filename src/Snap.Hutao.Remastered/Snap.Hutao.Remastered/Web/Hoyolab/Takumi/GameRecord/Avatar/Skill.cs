// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar;

internal sealed class Skill
{
    [JsonPropertyName("skill_id")]
    public SkillId SkillId { get; set; }

    [JsonPropertyName("skill_type")]
    public SkillType SkillType { get; set; }

    [JsonPropertyName("level")]
    public SkillLevel Level { get; set; }

    // Ignored field string desc
    // Ignored field * skill_affix_list
}