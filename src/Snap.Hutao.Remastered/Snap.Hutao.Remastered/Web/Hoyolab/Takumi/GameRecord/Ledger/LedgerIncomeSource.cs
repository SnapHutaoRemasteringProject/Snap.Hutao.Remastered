// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Ledger;

/// <summary>
/// 旅行者札记月度获取来源
/// </summary>
public sealed class LedgerIncomeSource
{
    [JsonPropertyName("action_id")]
    public int ActionId { get; init; }

    [JsonPropertyName("action")]
    public string Action { get; init; } = default!;

    [JsonPropertyName("num")]
    public int Num { get; init; }

    [JsonPropertyName("percent")]
    public int Percent { get; init; }
}
