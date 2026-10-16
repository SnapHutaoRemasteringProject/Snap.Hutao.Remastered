// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Calculable;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

public sealed partial class SkillView : NameIconDescription, ITypedCalculableSource<ICalculableSkill, SkillType>
{
    [ObservableProperty]
    public partial LevelParameters<string, ParameterDescription> Info { get; set; } = default!;

    [ObservableProperty]
    public partial uint LevelNumber { get; set; }

    [ObservableProperty]
    public partial string Level { get; set; } = default!;

    public SkillGroupId GroupId { get; set => SetProperty(ref field, value); }

    [ObservableProperty]
    public partial string? SpecialDescription { get; set; }

    public ICalculableSkill ToCalculable(SkillType type)
    {
        return CalculableSkill.From(this, type);
    }
}
