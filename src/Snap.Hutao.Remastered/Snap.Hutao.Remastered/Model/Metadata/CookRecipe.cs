// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata;

public sealed class CookRecipe : IDefaultIdentity<CookRecipeId>
{
    public required CookRecipeId Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required ImmutableArray<string> EffectDescription { get; init; }

    public required CookMethod CookMethod { get; init; }

    public required CookFoodType FoodType { get; init; }

    public required string Icon { get; init; }

    public required bool IsDefaultUnlocked { get; init; }

    public required uint MaxProficiency { get; init; }

    public required QualityType RankLevel { get; init; }

    public required ImmutableArray<IdCount> QualityOutput { get; init; }

    public required ImmutableArray<IdCount> Input { get; init; }
}
