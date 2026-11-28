// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Furniture;

public sealed class FurnitureType
{
    public FurnitureTypeId Id { get; init; }

    public uint Category { get; init; }

    public string Name { get; init; }

    public string Name2 { get; init; }

    public string TabIcon { get; init; }

    public FurnitureDeployType SceneType { get; init; }

    public bool BagPageOnly { get; init; }

    public bool IsShowInBag { get; init; }

    public uint Sort { get; init; }
}
