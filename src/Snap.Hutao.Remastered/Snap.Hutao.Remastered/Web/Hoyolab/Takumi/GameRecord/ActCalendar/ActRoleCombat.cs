// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

public sealed partial class ActRoleCombat : Act
{
    [JsonPropertyName("role_combat_detail")]
    public required ActRoleCombatDetail RoleCombatDetail { get; init; }
}