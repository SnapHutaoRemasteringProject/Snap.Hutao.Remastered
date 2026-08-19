// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Ledger;

/// <summary>
/// 旅行者札记当日获取数据
/// </summary>
public sealed class LedgerDayData
{
    [JsonPropertyName("current_primogems")]
    public int CurrentPrimogems { get; init; }

    [JsonPropertyName("current_mora")]
    public int CurrentMora { get; init; }

    [JsonPropertyName("last_primogems")]
    public int LastPrimogems { get; init; }

    [JsonPropertyName("last_mora")]
    public int LastMora { get; init; }

    [JsonIgnore]
    public string FormattedPrimogems => CurrentPrimogems.ToString("N0");

    [JsonIgnore]
    public string FormattedMora => CurrentMora.ToString("N0");
}
