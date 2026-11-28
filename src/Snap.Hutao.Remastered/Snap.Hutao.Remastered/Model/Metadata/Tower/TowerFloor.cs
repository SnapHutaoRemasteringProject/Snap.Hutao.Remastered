// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Tower;

[ProtoContract]
public sealed class TowerFloor : IDefaultIdentity<TowerFloorId>
{
    [ProtoMember(1)]
    public TowerFloorId Id { get; init; }

    [ProtoMember(2)]
    public uint Index { get; init; }

    [ProtoMember(3)]
    public TowerLevelGroupId LevelGroupId { get; init; }

    [ProtoMember(4)]
    public string Background { get; init; }

    [ProtoMember(5)]
    public ImmutableArray<string> Descriptions { get; init; } = [];

    [ProtoMember(6)]
    public ImmutableArray<string> FirstDescriptions { get; init; } = [];

    [ProtoMember(7)]
    public ImmutableArray<string> SecondDescriptions { get; init; } = [];
}
