// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata;

public sealed class NameCard
{
    public NameCardId Id { get; init; }

    public QualityType RankLevel { get; init; }

    public ItemType ItemType { get; init; }

    public string Name { get; init; }

    public string Description { get; init; }

    public string Icon { get; init; }

    public ImmutableArray<string> Pictures { get; init; } = [];
}
