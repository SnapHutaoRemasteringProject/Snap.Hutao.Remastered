// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Hutao;

internal sealed class GitRepository
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("web_url")]
    public required Uri WebUrl { get; set; }

    [JsonPropertyName("https_url")]
    public required Uri HttpsUrl { get; set; }

    [JsonPropertyName("ssh_url")]
    public Uri? SshUrl { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required GitRepositoryType Type { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}