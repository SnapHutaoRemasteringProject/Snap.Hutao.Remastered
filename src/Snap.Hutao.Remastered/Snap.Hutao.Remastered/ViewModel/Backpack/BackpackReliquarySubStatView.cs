// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

public sealed class BackpackReliquarySubStatView
{
    public static readonly BackpackReliquarySubStatView Empty = new() { HasValue = false };

    public FightProperty FightProp { get; init; }

    public float Value { get; init; }

    public uint EnhancedCount { get; init; }

    public bool HasValue { get; init; } = true;

    public string DisplayName => HasValue ? FightProp.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)! : string.Empty;

    public string DisplayValue => HasValue
        ? (FightProp.IsFightPropPercent()
            ? Value.ToString("P1", CultureInfo.CurrentCulture)
            : Value.ToString("F0", CultureInfo.CurrentCulture))
        : string.Empty;
}
