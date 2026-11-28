// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Tower;

[ProtoContract]
public sealed class TowerWave
{
    [ProtoMember(1)]
    public WaveType Type { get; init; }

    [ProtoMember(2)]
    public string? Description { get; init; }

    [ProtoMember(3)]
    public ImmutableArray<TowerMonster> Monsters { get; init; } = [];
}
