// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Reliquary;

[ProtoContract]
public sealed class ReliquarySet
{
    [ProtoMember(1)]
    public ReliquarySetId SetId { get; init; }

    [ProtoMember(2)]
    public EquipAffixId EquipAffixId { get; init; }

    [ProtoMember(3)]
    public ImmutableHashSet<ExtendedEquipAffixId> EquipAffixIds { get; init; } = [];

    [ProtoMember(5)]
    public string Name { get; init; }

    [ProtoMember(6)]
    public string Icon { get; init; }

    [ProtoMember(4)]
    public ImmutableArray<int> NeedNumber { get; init; } = [];

    [ProtoMember(7)]
    public ImmutableArray<string> Descriptions { get; init; } = [];
}
