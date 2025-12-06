// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;

internal sealed class RoleCombatAvatarStatistics
{
    [JsonPropertyName("avatar_id")]
    public required AvatarId AvatarId { get; init; }

    [JsonPropertyName("avatar_icon")]
    public required string AvatarIcon { get; init; }

    // Can be "" for 0
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("rarity")]
    public required QualityType Rarity { get; init; }
}