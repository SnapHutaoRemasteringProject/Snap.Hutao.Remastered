// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActExploreDetail
{
    [JsonPropertyName("explore_percent")]
    public required int ExplorePercent { get; init; }

    [JsonIgnore]
    public string FormattedExplorePercent { get => $"{ExplorePercent}%"; }
}