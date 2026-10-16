// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

public partial class Skill : ObservableObject
{
    public required SkillId Id { get; init => SetProperty(ref field, value); }

    public required string Name { get; init => SetProperty(ref field, value); }

    public required string Description { get; init => SetProperty(ref field, value); }

    public string? SpecialDescription { get; init => SetProperty(ref field, value); }

    public required string Icon { get; init => SetProperty(ref field, value); }

    public ExtraLevelInfo? ExtraLevel { get; init => SetProperty(ref field, value); }
}
