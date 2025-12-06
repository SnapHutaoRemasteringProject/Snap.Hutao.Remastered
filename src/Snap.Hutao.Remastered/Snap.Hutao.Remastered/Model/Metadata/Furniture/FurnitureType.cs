// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Furniture;

internal sealed class FurnitureType
{
    public required FurnitureTypeId Id { get; init; }

    public required uint Category { get; init; }

    public required string Name { get; init; }

    public required string Name2 { get; init; }

    public required string TabIcon { get; init; }

    public required FurnitureDeployType SceneType { get; init; }

    public required bool BagPageOnly { get; init; }

    public required bool IsShowInBag { get; init; }

    public required uint Sort { get; init; }
}