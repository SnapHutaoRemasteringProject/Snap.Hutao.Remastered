// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class Chapter
{
    [ProtoMember(1)]
    public ChapterId Id { get; init; }

    [ProtoMember(2)]
    public ChapterGroupId GroupId { get; init; }

    [ProtoMember(3)]
    public QuestId BeginQuestId { get; init; }

    [ProtoMember(4)]
    public QuestId EndQuestId { get; init; }

    [ProtoMember(5)]
    public uint NeedPlayerLevel { get; init; }

    [ProtoMember(6)]
    public string? Number { get; init; }

    [ProtoMember(7)]
    public string? Title { get; init; }

    [ProtoMember(8)]
    public string Icon { get; init; }

    [ProtoMember(9)]
    public string ChapterIcon { get; init; }

    [ProtoMember(10)]
    public string? ImageTitle { get; init; }

    [ProtoMember(11)]
    public string SerialNumberIcon { get; init; }

    [ProtoMember(12)]
    public City CityId { get; init; }

    [ProtoMember(13)]
    public QuestType QuestType { get; init; }
}
