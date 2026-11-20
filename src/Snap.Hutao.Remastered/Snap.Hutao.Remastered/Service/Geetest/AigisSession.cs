// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Geetest;

public sealed class AigisSession
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = default!;

    [JsonPropertyName("mmt_type")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int MmtType { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}
