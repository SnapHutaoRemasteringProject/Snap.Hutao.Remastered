// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActLiBenDetail
{
    [JsonPropertyName("status")]
    public required ActLiBenDetailStatus Status { get; init; }

    [JsonPropertyName("progress")]
    public required int Progress { get; init; }

    [JsonPropertyName("total")]
    public required int Total { get; init; }

    // 大礼盒已领取
    [JsonPropertyName("is_has_taken_special_reward")]
    public required bool IsHasTakenSpecialReward { get; init; }
}