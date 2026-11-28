// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Calculable;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
public sealed partial class ProudSkill : ObservableObject, ITypedCalculableSource<ICalculableSkill, SkillType>, ISkillNameIcon
{
    [ProtoMember(3)]
    public SkillId Id { get; init => SetProperty(ref field, value); }

    [ProtoMember(4)]
    public string Name { get; init => SetProperty(ref field, value); }

    [ProtoMember(5)]
    public string Description { get; init => SetProperty(ref field, value); }

    [ProtoMember(6)]
    public string? SpecialDescription { get; init => SetProperty(ref field, value); }

    [ProtoMember(8)]
    public string Icon { get; init => SetProperty(ref field, value); }

    [ProtoMember(1)]
    public SkillGroupId GroupId { get; init; }

    [ProtoMember(2)]
    public DescriptionsParameters Proud { get; init; }

    [ProtoMember(7)]
    public EnergyType SpecialEnergyType { get; init; }

    [ObservableProperty]
    [ProtoMember(9)]
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
