// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class ReliquaryView : EquipView
{
    [ObservableProperty]
    public partial ImmutableArray<ReliquaryComposedSubProperty> ComposedSubProperties { get; set; } = [];

    [ObservableProperty]
    public partial string SetName { get; set; } = default!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Score))]
    [NotifyPropertyChangedFor(nameof(ScoreColorValue))]
    public partial double ScoreValue { get; private set; }

    public string Score => string.Format(SH.ViewPageAvatarPropertyReliquaryScoreValue, ScoreValue);

    public int ScoreColorValue => (int)Math.Round(ScoreValue);

    public void SetScoreValue(double value)
    {
        ScoreValue = value;
    }
}
