// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Calculable;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

public sealed partial class ProudSkill : Skill, ITypedCalculableSource<ICalculableSkill, SkillType>
{
    public required SkillGroupId GroupId { get; init; }

    public required DescriptionsParameters Proud { get; init; }

    public required EnergyType SpecialEnergyType { get; init; }

    [ObservableProperty]
    public partial float? CdTime { get; set; }

    public static uint GetMaxLevel()
    {
        return 10U;
    }

    public ICalculableSkill ToCalculable(SkillType type)
    {
        return CalculableSkill.From(this, type);
    }
}
