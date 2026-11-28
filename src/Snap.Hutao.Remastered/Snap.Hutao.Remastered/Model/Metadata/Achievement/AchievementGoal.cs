// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Achievement;

[ProtoContract]
public sealed class AchievementGoal
{
    [ProtoMember(1)]
    public AchievementGoalId Id { get; init; }

    [ProtoMember(2)]
    public uint Order { get; init; }

    [ProtoMember(3)]
    public string Name { get; init; }

    [ProtoMember(4)]
    public Reward? FinishReward { get; init; }

    [ProtoMember(5)]
    public string Icon { get; init; }
}
