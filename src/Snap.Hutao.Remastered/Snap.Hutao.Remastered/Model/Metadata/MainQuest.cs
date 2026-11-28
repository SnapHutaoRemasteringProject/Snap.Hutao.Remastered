// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class MainQuest
{
    [ProtoMember(1)]
    public MainQuestId Id { get; init; }

    [ProtoMember(2)]
    public QuestType Type { get; init; }

    [ProtoMember(3)]
    public string Title { get; init; }

    [ProtoMember(4)]
    public string Description { get; init; }

    [ProtoMember(5)]
    public string? UnlockDescription { get; init; }

    [ProtoMember(6)]
    public ChapterId ChapterId { get; init; }

    [ProtoMember(7)]
    public uint SortWeight { get; init; }

    [ProtoMember(8)]
    public uint RecommendLevel { get; init; }

    [ProtoMember(9)]
    public uint ActivityId { get; init; }

    [ProtoMember(10)]
    public MainQuestTag MainQuestTag { get; init; }

    [ProtoMember(11)]
    public QuestShowType ShowType { get; init; }

    [ProtoMember(12)]
    public bool Repeatable { get; init; }

    [ProtoMember(13)]
    public uint Series { get; init; }

    [ProtoMember(14)]
    public uint TaskId { get; init; }

    [ProtoMember(15)]
    public ImmutableArray<IdCount> RewardList { get; init; } = [];
}
