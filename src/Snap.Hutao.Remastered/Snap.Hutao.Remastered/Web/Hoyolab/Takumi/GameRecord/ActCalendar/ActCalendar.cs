// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActCalendar
{
    [JsonPropertyName("avatar_card_pool_list")]
    public required ImmutableArray<CardPool> AvatarCardPoolList { get; init; } = [];

    [JsonPropertyName("weapon_card_pool_list")]
    public required ImmutableArray<CardPool> WeaponCardPoolList { get; init; } = [];

    [JsonPropertyName("mixed_card_pool_list")]
    public required ImmutableArray<CardPool> MixedCardPoolList { get; init; } = [];

    [JsonPropertyName("selected_avatar_card_pool_list")]
    public required ImmutableArray<CardPool> SelectedAvatarCardPoolList { get; init; }

    [JsonPropertyName("selected_mixed_card_pool_list")]
    public required ImmutableArray<CardPool> SelectedMixedCardPoolList { get; init; }

    [JsonIgnore]
    public ImmutableArray<CardPool> CompositeCardPools { get => !field.IsDefault ? field : field = [.. AvatarCardPoolList, .. WeaponCardPoolList, .. MixedCardPoolList]; }

    [JsonPropertyName("act_list")]
    public required ImmutableArray<Act> ActList { get; init; }

    [JsonPropertyName("fixed_act_list")]
    public required ImmutableArray<Act> FixedActList { get; init; }

    [JsonPropertyName("selected_act_list")]
    public required ImmutableArray<Act> SelectedActList { get; init; }

    [JsonIgnore]
    public ImmutableArray<Act> CompositeActs { get => !field.IsDefault ? field : field = [.. FixedActList, .. ActList]; }
}