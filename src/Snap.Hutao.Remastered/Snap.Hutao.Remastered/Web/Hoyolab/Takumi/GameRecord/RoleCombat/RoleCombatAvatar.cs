// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;

internal sealed class RoleCombatAvatar
{
    [JsonPropertyName("avatar_id")]
    public required AvatarId AvatarId { get; init; }

    [JsonPropertyName("avatar_type")]
    public required RoleCombatAvatarType AvatarType { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("element")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ElementName Element { get; init; }

    [JsonPropertyName("image")]
    public required Uri Image { get; init; }

    [JsonPropertyName("level")]
    public required Level Level { get; init; }

    [JsonPropertyName("rarity")]
    public required QualityType Rarity { get; init; }
}