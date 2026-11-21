// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Geetest;

public sealed class GeetestWebResponse
{
    [JsonConstructor]
    public GeetestWebResponse(string challenge, string validate)
    {
        Challenge = challenge;
        Validate = validate;
        Seccode = $"{validate}|jordan";
    }

    [JsonPropertyName("geetest_challenge")]
    public string Challenge { get; set; }

    [JsonPropertyName("geetest_validate")]
    public string Validate { get; set; }

    [JsonPropertyName("geetest_seccode")]
    public string Seccode { get; set; }
}