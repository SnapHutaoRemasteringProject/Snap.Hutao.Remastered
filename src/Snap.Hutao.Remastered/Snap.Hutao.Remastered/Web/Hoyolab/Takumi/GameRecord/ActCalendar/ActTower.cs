// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActTower : Act
{
    [JsonPropertyName("tower_detail")]
    public required ActTowerDetail TowerDetail { get; init; }
}