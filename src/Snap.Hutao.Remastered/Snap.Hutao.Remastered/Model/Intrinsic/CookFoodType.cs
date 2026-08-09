// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model.Intrinsic;

[ExtendedEnum]
public enum CookFoodType
{
    COOK_FOOD_NONE,

    [LocalizationKey(nameof(SH.ModelIntrinsicCookFoodTypeHeal))]
    COOK_FOOD_HEAL,

    [LocalizationKey(nameof(SH.ModelIntrinsicCookFoodTypeAttack))]
    COOK_FOOD_ATTACK,

    [LocalizationKey(nameof(SH.ModelIntrinsicCookFoodTypeFunction))]
    COOK_FOOD_FUNCTION,

    [LocalizationKey(nameof(SH.ModelIntrinsicCookFoodTypeDefense))]
    COOK_FOOD_DEFENSE,

    [LocalizationKey(nameof(SH.ModelIntrinsicCookFoodTypeRecipe))]
    COOK_RECIPE,
}
