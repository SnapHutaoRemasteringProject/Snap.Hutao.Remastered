// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActHardChallengeDetailSub
{
    [JsonPropertyName("seconds")]
    public required int Seconds { get; init; }

    [JsonPropertyName("x")]
    public required int X { get; init; }

    [JsonPropertyName("y")]
    public required int Y { get; init; }
}