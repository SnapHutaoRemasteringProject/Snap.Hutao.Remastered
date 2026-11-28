// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class HardChallengeSchedule : IDefaultIdentity<HardChallengeScheduleId>
{
    [ProtoMember(1)]
    public HardChallengeScheduleId Id { get; init; }

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
    public string Name { get; init; }
}
