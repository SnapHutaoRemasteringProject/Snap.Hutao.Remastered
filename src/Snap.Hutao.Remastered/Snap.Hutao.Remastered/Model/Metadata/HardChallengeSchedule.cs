// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata;

internal sealed class HardChallengeSchedule : IDefaultIdentity<HardChallengeScheduleId>
{
    public required HardChallengeScheduleId Id { get; init; }

    public required DateTimeOffset Begin { get; init; }

    public required DateTimeOffset End { get; init; }

    public required string Name { get; init; }
}