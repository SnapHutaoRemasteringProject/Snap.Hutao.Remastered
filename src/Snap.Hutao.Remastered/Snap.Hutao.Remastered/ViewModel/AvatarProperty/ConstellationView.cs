// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

public sealed partial class ConstellationView : NameIconDescription
{
    [ObservableProperty]
    public partial bool IsActivated { get; set; }

    [ObservableProperty]
    public partial string? SpecialDescription { get; set; }
}
