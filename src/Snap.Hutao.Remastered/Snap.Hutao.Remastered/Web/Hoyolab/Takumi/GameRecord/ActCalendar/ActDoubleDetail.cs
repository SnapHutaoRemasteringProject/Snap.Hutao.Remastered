// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActDoubleDetail
{
    // 剩余双倍次数
    [JsonPropertyName("left")]
    public required int Left { get; init; }

    [JsonPropertyName("total")]
    public required int Total { get; init; }
}