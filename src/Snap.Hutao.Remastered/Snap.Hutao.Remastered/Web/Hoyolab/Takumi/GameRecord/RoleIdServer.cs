// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord;

internal sealed class RoleIdServer
{
    public RoleIdServer(PlayerUid uid)
    {
        RoleId = uid.Value;
        Server = uid.Region.Value;
    }

    [JsonPropertyName("role_id")]
    public string RoleId { get; }

    [JsonPropertyName("server")]
    public string Server { get; }
}