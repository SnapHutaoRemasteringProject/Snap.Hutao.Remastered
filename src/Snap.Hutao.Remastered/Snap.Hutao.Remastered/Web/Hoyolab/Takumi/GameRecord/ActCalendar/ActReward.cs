// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActReward
{
    [JsonPropertyName("item_id")]
    public required MaterialId ItemId { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("icon")]
    public required Uri Icon { get; init; }

    [JsonPropertyName("wiki_url")]
    public required Uri WikiUrl { get; set; }

    [JsonPropertyName("num")]
    public required int Number { get; init; }

    [JsonPropertyName("rarity")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required QualityType Rarity { get; init; }

    [JsonPropertyName("homepage_show")]
    public required bool HomePageShow { get; set; }
}