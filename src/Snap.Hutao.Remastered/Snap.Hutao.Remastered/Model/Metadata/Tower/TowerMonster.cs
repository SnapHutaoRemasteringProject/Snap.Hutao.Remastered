// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Tower;

[ProtoContract]
public sealed class TowerMonster
{
    [ProtoMember(3)]
    public MonsterDescribeId Id { get; init; }

    [ProtoMember(4)]
    public uint Count { get; init; }

    [ProtoMember(1)]
    public bool AttackMonolith { get; init; }

    [ProtoMember(2)]
    public ImmutableArray<NameDescription> Affixes { get; init; } = [];
}
