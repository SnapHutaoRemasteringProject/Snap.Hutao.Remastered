// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class GachaEvent
{
    [ProtoMember(1)]
    public string Name { get; init; }

    [ProtoMember(2)]
    public string Version { get; init; }

    [ProtoMember(3)]
    public uint Order { get; init; }

    [ProtoMember(4)]
    public Uri Banner { get; init; }

    [ProtoIgnore]
    public Uri? Banner2 { get; init; }

    [JsonIgnore]
    [ProtoMember(5)]
    public string? Banner2Wire
    {
        get => Banner2?.OriginalString;
        init => Banner2 = string.IsNullOrEmpty(value) ? null : Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ? uri : null;
    }

    [ProtoIgnore]
    public DateTimeOffset From { get; init; }

    [JsonIgnore]
    [ProtoMember(6)]
    public string FromWire
    {
        get => MetadataProtoTime.ToWire(From);
        init => From = MetadataProtoTime.FromWire(value);
    }

    [ProtoIgnore]
    public DateTimeOffset To { get; init; }

    [JsonIgnore]
    [ProtoMember(7)]
    public string ToWire
    {
        get => MetadataProtoTime.ToWire(To);
        init => To = MetadataProtoTime.FromWire(value);
    }

    [ProtoMember(8)]
    public GachaType Type { get; init; }

    // protobuf-net leaves a missing repeated field at its default value, so an empty up list on
    // the producer side arrives as null unless it is initialized here (see CLAUDE.md §5.5).
    [ProtoMember(9)]
    public HashSet<uint> UpOrangeList { get; init; } = [];

    [ProtoMember(10)]
    public HashSet<uint> UpPurpleList { get; init; } = [];
}
