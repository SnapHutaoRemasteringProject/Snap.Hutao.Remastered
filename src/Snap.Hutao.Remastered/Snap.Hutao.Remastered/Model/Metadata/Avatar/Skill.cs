// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
public partial class Skill : ObservableObject, ISkillNameIcon
{
    [ProtoMember(1)]
    public SkillId Id { get; init => SetProperty(ref field, value); }

    [ProtoMember(2)]
    public string Name { get; init => SetProperty(ref field, value); }

    [ProtoMember(3)]
    public string Description { get; init => SetProperty(ref field, value); }

    [ProtoMember(4)]
    public string? SpecialDescription { get; init => SetProperty(ref field, value); }

    [ProtoMember(5)]
    public string Icon { get; init => SetProperty(ref field, value); }

    [ProtoMember(6)]
    public ExtraLevelInfo? ExtraLevel { get; init => SetProperty(ref field, value); }
}
