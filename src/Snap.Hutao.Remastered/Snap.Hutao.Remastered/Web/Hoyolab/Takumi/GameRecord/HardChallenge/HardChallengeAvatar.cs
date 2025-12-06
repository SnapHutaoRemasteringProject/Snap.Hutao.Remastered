// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;

internal sealed class HardChallengeAvatar : HardChallengeSimpleAvatar
{
    [JsonPropertyName("level")]
    public required Level Level { get; init; }

    /// <summary>
    /// 实际上是命座
    /// </summary>
    [JsonPropertyName("rank")]
    public required int Rank { get; init; }
}