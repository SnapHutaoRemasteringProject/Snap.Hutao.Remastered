// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

public sealed class ReliquaryView : EquipView
{
    public ImmutableArray<ReliquaryComposedSubProperty> ComposedSubProperties { get; set; }

    public string SetName { get; set; } = default!;

    public double ScoreValue { get; set; }

    public string Score { get; set; } = default!;

    public int ScoreColorValue => (int)Math.Round(ScoreValue);
}