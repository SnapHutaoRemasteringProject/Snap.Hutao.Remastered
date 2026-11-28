// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Tower;

[ProtoContract]
public sealed class TowerSchedule : IDefaultIdentity<TowerScheduleId>
{
    [ProtoMember(1)]
    public TowerScheduleId Id { get; init; }

    [ProtoMember(2)]
    public ImmutableArray<TowerFloorId> FloorIds { get; init; } = [];

    [ProtoIgnore]
    public DateTimeOffset Open { get; init; }

    [JsonIgnore]
    [ProtoMember(3)]
    public string OpenWire
    {
        get => MetadataProtoTime.ToWire(Open);
        init => Open = MetadataProtoTime.FromWire(value);
    }

    [ProtoIgnore]
    public DateTimeOffset Close { get; init; }

    [JsonIgnore]
    [ProtoMember(4)]
    public string CloseWire
    {
        get => MetadataProtoTime.ToWire(Close);
        init => Close = MetadataProtoTime.FromWire(value);
    }

    [ProtoMember(5)]
    public string BuffName { get; init; }

    [ProtoMember(6)]
    public ImmutableArray<string> Descriptions { get; init; } = [];

    [ProtoMember(7)]
    public string Icon { get; init; }
}
