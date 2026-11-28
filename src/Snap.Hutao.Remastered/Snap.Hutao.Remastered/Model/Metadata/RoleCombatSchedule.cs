// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class RoleCombatSchedule : IDefaultIdentity<RoleCombatScheduleId>
{
    [ProtoMember(1)]
    public RoleCombatScheduleId Id { get; init; }

    [ProtoIgnore]
    public DateTimeOffset Begin { get; init; }

    [JsonIgnore]
    [ProtoMember(2)]
    public string BeginWire
    {
        get => MetadataProtoTime.ToWire(Begin);
        init => Begin = MetadataProtoTime.FromWire(value);
    }

    [ProtoIgnore]
    public DateTimeOffset End { get; init; }

    [JsonIgnore]
    [ProtoMember(3)]
    public string EndWire
    {
        get => MetadataProtoTime.ToWire(End);
        init => End = MetadataProtoTime.FromWire(value);
    }

    [ProtoMember(4)]
    public ImmutableArray<ElementType> Elements { get; init; } = [];

    [ProtoMember(5)]
    public ImmutableArray<AvatarId> SpecialAvatars { get; init; } = [];

    [ProtoMember(6)]
    public ImmutableArray<AvatarId> InitialAvatars { get; init; } = [];
}
