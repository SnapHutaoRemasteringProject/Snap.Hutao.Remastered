// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Reliquary;

[ProtoContract]
public sealed class Reliquary
{
    [ProtoMember(1)]
    public ImmutableArray<ReliquaryId> Ids { get; init; } = [];

    [ProtoMember(2)]
    public QualityType RankLevel { get; init; }

    [ProtoMember(3)]
    public ReliquarySetId SetId { get; init; }

    [ProtoMember(4)]
    public EquipType EquipType { get; init; }

    [ProtoMember(5)]
    public ItemType ItemType { get; init; }

    [ProtoMember(6)]
    public string Name { get; init; }

    [ProtoMember(7)]
    public string Description { get; init; }

    [ProtoMember(8)]
    public string Icon { get; init; }

    [ProtoMember(9)]
    public uint SortOrder { get; init; }
}
