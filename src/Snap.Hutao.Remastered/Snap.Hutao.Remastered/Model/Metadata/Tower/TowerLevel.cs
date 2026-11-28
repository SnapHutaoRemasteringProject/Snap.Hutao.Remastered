// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Tower;

[ProtoContract]
public sealed class TowerLevel
{
    [ProtoMember(1)]
    public TowerLevelId Id { get; init; }

    [ProtoMember(2)]
    public TowerLevelGroupId GroupId { get; init; }

    [ProtoMember(3)]
    public uint Index { get; init; }

    [ProtoMember(4)]
    public uint MonsterLevel { get; init; }

    [ProtoMember(6)]
    public ImmutableArray<MonsterDescribeId> FirstMonsters { get; init; } = [];

    [ProtoMember(8)]
    public ImmutableArray<TowerWave> FirstWaves { get; init; } = [];

    [ProtoMember(9)]
    public NameDescription? FirstGadget { get; init; }

    [ProtoMember(7)]
    public ImmutableArray<MonsterDescribeId> SecondMonsters { get; init; } = [];

    [ProtoMember(10)]
    public ImmutableArray<TowerWave> SecondWaves { get; init; } = [];

    [ProtoMember(11)]
    public NameDescription? SecondGadget { get; init; }
}
