// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Ledger;

/// <summary>
/// 旅行者札记月度累计数据
/// </summary>
public sealed class LedgerMonthData
{
    [JsonPropertyName("current_primogems")]
    public int CurrentPrimogems { get; init; }

    [JsonPropertyName("current_mora")]
    public int CurrentMora { get; init; }

    [JsonPropertyName("last_primogems")]
    public int LastPrimogems { get; init; }

    [JsonPropertyName("last_mora")]
    public int LastMora { get; init; }

    [JsonPropertyName("current_primogems_level")]
    public int CurrentPrimogemsLevel { get; init; }

    [JsonPropertyName("primogems_rate")]
    public int PrimogemsRate { get; init; }

    [JsonPropertyName("mora_rate")]
    public int MoraRate { get; init; }

    [JsonPropertyName("group_by")]
    public List<LedgerIncomeSource> GroupBy { get; init; } = [];

    [JsonIgnore]
    public string FormattedPrimogems => CurrentPrimogems.ToString("N0");

    [JsonIgnore]
    public string FormattedMora => CurrentMora.ToString("N0");
}
