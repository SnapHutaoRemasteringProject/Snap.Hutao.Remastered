// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json;

namespace Snap.Hutao.Remastered.Web.Bridge.Model;

public sealed class JsParam
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = default!;

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; set; }

    [JsonPropertyName("callback")]
    public string? Callback { get; set; }
}

[SuppressMessage("", "SA1402")]
public sealed class JsParam<TPayload>
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = default!;

    [JsonPropertyName("payload")]
    public TPayload Payload { get; set; } = default!;

    [JsonPropertyName("callback")]
    public string? Callback { get; set; }

    public static implicit operator JsParam<TPayload>(JsParam jsParam)
    {
        return new()
        {
            Method = jsParam.Method,
            Payload = JsonSerialize.Deserialize<TPayload>(jsParam.Payload, JsonOptions.Default) ?? default!,
            Callback = jsParam.Callback,
        };
    }
}