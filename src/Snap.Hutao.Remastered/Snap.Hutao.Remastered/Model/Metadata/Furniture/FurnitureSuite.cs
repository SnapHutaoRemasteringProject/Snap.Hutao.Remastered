// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Furniture;

public sealed class FurnitureSuite
{
    public FurnitureSuiteId Id { get; init; }

    public ImmutableArray<FurnitureTypeId> Types { get; init; } = [];

    public string Name { get; init; }

    public string Description { get; init; }

    public string ItemIcon { get; init; }

    public string? MapIcon { get; init; }

    public ImmutableArray<AvatarId> FavoriteNpcs { get; init; } = [];

    public ImmutableArray<FurnitureId> Units { get; init; } = [];
}
