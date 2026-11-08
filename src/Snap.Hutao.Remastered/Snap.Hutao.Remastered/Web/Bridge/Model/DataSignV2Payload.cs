// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Bridge.Model;

public sealed class DataSignV2Payload
{
    [JsonPropertyName("query")]
    public Dictionary<string, JsonElement> Query { get; set; } = default!;

    [JsonPropertyName("body")]
    public string Body { get; set; } = default!;

    [SuppressMessage("", "CA1308")]
    public string GetQueryParam()
    {
        return string.Join('&', Query.OrderBy(x => x.Key).Select(x => x.Value.GetRawText()));
    }
}