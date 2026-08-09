// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.UI.Xaml.Control.AutoSuggestBox;
using System.Collections.Frozen;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

public static class BackpackFilter
{
    public static Predicate<BackpackItemView>? Compile(
        SearchData? searchData,
        double? level,
        FrozenDictionary<uint, int> foodQualityMap,
        FrozenDictionary<uint, CookFoodType> foodTypeMap)
    {
        Predicate<BackpackItemView>? tokenPredicate = searchData is { FilterTokens.Count: > 0 }
            ? Compile(searchData.FilterTokens, foodQualityMap, foodTypeMap)
            : null;

        if (level.HasValue && !double.IsNaN(level.Value))
        {
            uint targetLevel = (uint)level.Value;
            bool levelPredicate(BackpackItemView item) => item switch
            {
                BackpackWeaponItemView w => w.Level == targetLevel,
                BackpackReliquaryItemView r => r.Level == targetLevel,
                _ => false,
            };

            return tokenPredicate is null
                ? levelPredicate
                : item => tokenPredicate(item) && levelPredicate(item);
        }

        return tokenPredicate;
    }

    private static Predicate<BackpackItemView> Compile(
        IEnumerable<SearchToken> input,
        FrozenDictionary<uint, int> foodQualityMap,
        FrozenDictionary<uint, CookFoodType> foodTypeMap)
    {
        ILookup<SearchTokenKind, string> lookup = input.ToLookup(token => token.Kind, token => token.Value);
        return item => Compile(lookup, item, foodQualityMap, foodTypeMap);
    }

    private static bool Compile(
        ILookup<SearchTokenKind, string> lookup,
        BackpackItemView item,
        FrozenDictionary<uint, int> foodQualityMap,
        FrozenDictionary<uint, CookFoodType> foodTypeMap)
    {
        // Use short-circuit evaluation to avoid per-item List<bool> allocations
        bool anyChecked = false;

        foreach ((SearchTokenKind kind, IEnumerable<string> tokens) in lookup)
        {
            anyChecked = true;

            bool matches = kind switch
            {
                SearchTokenKind.None => tokens.Any(token => item.Name.Contains(token, StringComparison.OrdinalIgnoreCase)),

                SearchTokenKind.WeaponType => item is not BackpackWeaponItemView || (item is BackpackWeaponItemView w && tokens.Contains(w.WeaponTypeName)),

                SearchTokenKind.ItemQuality or SearchTokenKind.BackpackQuality => tokens.Contains(
                    (item switch
                    {
                        BackpackWeaponItemView wv => wv.Weapon.RankLevel,
                        BackpackReliquaryItemView rq => rq.Reliquary.RankLevel,
                        _ when item.Material is not null => item.Material.RankLevel,
                        _ => QualityType.QUALITY_NONE,
                    }).GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)),

                SearchTokenKind.BackpackLockState => tokens.Contains(
                    item.Entity.IsLocked
                        ? SH.ViewPageBackpackFilterLocked
                        : SH.ViewPageBackpackFilterUnlocked),

                SearchTokenKind.BackpackMarkState => item is not BackpackReliquaryItemView ||
                    (item is BackpackReliquaryItemView r &&
                    tokens.Contains(r.IsMarked
                        ? SH.ViewPageBackpackFilterMarked
                        : SH.ViewPageBackpackFilterUnmarked)),

                SearchTokenKind.BackpackFoodQuality => !foodQualityMap.TryGetValue(item.Entity.ItemId, out int qualityIndex) ||
                    tokens.Contains(qualityIndex switch
                    {
                        0 => SH.ViewPageBackpackFilterFoodQualitySuspicious,
                        1 => SH.ViewPageBackpackFilterFoodQualityNormal,
                        2 => SH.ViewPageBackpackFilterFoodQualityDelicious,
                        _ => string.Empty,
                    }),

                SearchTokenKind.BackpackCookFoodType => !foodTypeMap.TryGetValue(item.Entity.ItemId, out CookFoodType foodType) ||
                    tokens.Contains(foodType.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)!),

                SearchTokenKind.BackpackReliquarySet => item is not BackpackReliquaryItemView ||
                    (item is BackpackReliquaryItemView rSet &&
                    tokens.Contains(rSet.SetName ?? string.Empty)),

                SearchTokenKind.BackpackEquipType => item is not BackpackReliquaryItemView ||
                    (item is BackpackReliquaryItemView rEquip &&
                    tokens.Contains(rEquip.EquipTypeName)),

                _ => false,
            };

            if (!matches)
            {
                return false; // Short-circuit: one mismatch is enough
            }
        }

        return anyChecked;
    }
}
