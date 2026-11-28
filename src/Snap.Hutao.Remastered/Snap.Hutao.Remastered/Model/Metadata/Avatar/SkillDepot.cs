// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
public sealed class SkillDepot
{
    [ProtoMember(1)]
    public Arkhe Arkhe { get; init; }

    [ProtoMember(2)]
    public ImmutableArray<ProudSkill> Skills { get; init; } = [];

    [ProtoMember(4)]
    public ProudSkill EnergySkill { get; init; }
    [ProtoMember(3)]
    public ImmutableArray<ProudSkill> SpecialSkills { get; init; } = [];

    [ProtoMember(5)]
    public ImmutableArray<ProudSkill> Inherents { get; init; } = [];

    [ProtoMember(6)]
    public ImmutableArray<Skill> Talents { get; init; } = [];

    public ImmutableArray<ProudSkill> CompositeSkills { get => !field.IsDefault ? field : field = [.. Skills, EnergySkill, .. SpecialSkills, .. Inherents]; }

    // No Inherents && 跳过 替换冲刺的技能
    public ImmutableArray<ProudSkill> CompositeSkillsNoInherents { get => !field.IsDefault ? field : field = [.. Skills.Where(s => s.Proud.Parameters.Count > 1), EnergySkill]; }
}
