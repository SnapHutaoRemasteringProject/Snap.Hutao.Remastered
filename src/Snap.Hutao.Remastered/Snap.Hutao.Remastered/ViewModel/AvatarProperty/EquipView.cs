// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

public abstract partial class EquipView : NameIconDescription
{
    [ObservableProperty]
    public partial string Level { get; set; } = default!;

    [ObservableProperty]
    public partial QualityType Quality { get; set; }

    [ObservableProperty]
    public partial NameValue<string>? MainProperty { get; set; }

    [ObservableProperty]
    public partial EquipType EquipType { get; set; }
}
