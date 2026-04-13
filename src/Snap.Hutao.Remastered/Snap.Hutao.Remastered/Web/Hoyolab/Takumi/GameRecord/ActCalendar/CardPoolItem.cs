// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal class CardPoolItem
{
    [JsonPropertyName("icon")]
    public required Uri Icon { get; set; }

    [JsonPropertyName("rarity")]
    public required QualityType Rarity { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}