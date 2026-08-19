// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Ledger;

/// <summary>
/// 旅行者札记月度获取数据
/// </summary>
public sealed class Ledger
{
    [JsonPropertyName("uid")]
    public int Uid { get; init; }

    [JsonPropertyName("region")]
    public string Region { get; init; } = default!;

    [JsonPropertyName("nickname")]
    public string Nickname { get; init; } = default!;

    [JsonPropertyName("date")]
    public string Date { get; init; } = default!;

    [JsonPropertyName("month")]
    public int Month { get; init; }

    [JsonPropertyName("optional_month")]
    public List<int> OptionalMonth { get; init; } = [];

    [JsonPropertyName("day_data")]
    public LedgerDayData DayData { get; init; } = default!;

    [JsonPropertyName("month_data")]
    public LedgerMonthData MonthData { get; init; } = default!;
}
