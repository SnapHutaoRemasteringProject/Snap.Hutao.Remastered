// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Furniture;

public sealed class FurnitureMake
{
    public FurnitureMakeId Id { get; init; }

    public FurnitureId ItemId { get; init; }

    public uint Experience { get; init; }

    public ImmutableArray<IdCount> Materials { get; init; } = [];
}
