// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

[WinRT.GeneratedBindableCustomProperty]
[ObservableObject]
public partial class ReliquaryComposedSubProperty : ReliquarySubProperty
{
    public ReliquaryComposedSubProperty(FightProperty type, string value, uint enhancedCount)
        : base(type, value)
    {
        Type = type;
        EnhancedCount = enhancedCount;
    }

    [ObservableProperty]
    public partial uint EnhancedCount { get; set; }

    public FightProperty Type { get; }
}
