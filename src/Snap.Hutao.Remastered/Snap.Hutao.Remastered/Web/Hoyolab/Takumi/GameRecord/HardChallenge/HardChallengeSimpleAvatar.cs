// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;

internal class HardChallengeSimpleAvatar
{
    [JsonPropertyName("avatar_id")]
    public required AvatarId AvatarId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("element")]
    public required string Element { get; init; }

    [JsonPropertyName("image")]
    public required string Image { get; init; }

    [JsonPropertyName("rarity")]
    public required QualityType Rarity { get; init; }
}