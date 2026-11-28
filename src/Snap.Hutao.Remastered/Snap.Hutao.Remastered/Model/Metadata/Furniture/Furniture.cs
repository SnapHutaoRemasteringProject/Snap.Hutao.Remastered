// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Furniture;

public sealed class Furniture
{
    public ImmutableArray<FurnitureTypeId> Types { get; init; } = [];

    public FurnitureDeploySurfaceType SurfaceType { get; init; }

    public bool IsSpecial { get; init; }

    public SpecialFurnitureType SpecialType { get; init; }

    public uint Comfort { get; init; }

    public uint Cost { get; init; }

    public uint DiscountCost { get; init; }

    public bool CanFloat { get; init; }

    public bool IsUnique { get; init; }

    public string? ItemIcon { get; init; }

    public string? EffectIcon { get; init; }

    public QualityType RankLevel { get; init; }

    public ImmutableArray<FurnitureId> GroupUnits { get; init; } = [];

    public GroupRecordType GroupRecordType { get; init; }

    public ImmutableArray<string> SourceTexts { get; init; } = [];

    public FurnitureId Id { get; init; }

    public string Name { get; init; }

    public string Description { get; init; }

    public string? Icon { get; init; }

    public uint Rank { get; init; }
}
