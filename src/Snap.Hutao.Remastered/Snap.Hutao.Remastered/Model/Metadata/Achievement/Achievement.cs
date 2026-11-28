// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Achievement;

[ProtoContract]
public sealed class Achievement : IDefaultIdentity<AchievementId>
{
    [ProtoMember(1)]
    public AchievementId Id { get; init; }

    [ProtoMember(2)]
    public AchievementGoalId Goal { get; init; }

    [ProtoMember(3)]
    public uint Order { get; init; }

    [ProtoMember(4)]
    public AchievementId PreviousId { get; init; }

    [ProtoMember(5)]
    public string Title { get; init; }

    [ProtoMember(6)]
    public string Description { get; init; }

    [ProtoMember(7)]
    public Reward FinishReward { get; init; }

    [ProtoMember(8)]
    public bool IsDeleteWatcherAfterFinish { get; init; }

    [ProtoMember(9)]
    public uint Progress { get; init; }

    [ProtoMember(10)]
    public string? Icon { get; init; }

    [ProtoMember(11)]
    public string Version { get; init; }

    [ProtoMember(12)]
    public bool IsDailyQuest { get; init; }
}
