// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActExplore : Act
{
    [JsonPropertyName("explore_detail")]
    public required ActExploreDetail ExploreDetail { get; init; }
}