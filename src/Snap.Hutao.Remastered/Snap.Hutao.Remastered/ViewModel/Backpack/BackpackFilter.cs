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
        List<bool> matches = [];

        foreach ((SearchTokenKind kind, IEnumerable<string> tokens) in lookup)
        {
            switch (kind)
            {
                case SearchTokenKind.None:
                    matches.Add(tokens.Any(token => item.Name.Contains(token, StringComparison.OrdinalIgnoreCase)));
                    break;

                case SearchTokenKind.WeaponType:
                    if (item is BackpackWeaponItemView w)
                    {
                        matches.Add(tokens.Contains(w.WeaponTypeName));
                    }

                    break;

                case SearchTokenKind.ItemQuality:
                case SearchTokenKind.BackpackQuality:
                    {
                        QualityType quality = item switch
                        {
                            BackpackWeaponItemView wv => wv.Weapon.RankLevel,
                            BackpackReliquaryItemView rq => rq.Reliquary.RankLevel,
                            _ when item.Material is not null => item.Material.RankLevel,
                            _ => QualityType.QUALITY_NONE,
                        };

                        matches.Add(tokens.Contains(quality.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)));
                        break;
                    }

                case SearchTokenKind.BackpackLockState:
                    {
                        bool isLocked = item.Entity.IsLocked;
                        string lockState = isLocked
                            ? SH.ViewPageBackpackFilterLocked
                            : SH.ViewPageBackpackFilterUnlocked;
                        matches.Add(tokens.Contains(lockState));
                        break;
                    }

                case SearchTokenKind.BackpackMarkState:
                    {
                        if (item is BackpackReliquaryItemView r)
                        {
                            string markState = r.IsMarked
                                ? SH.ViewPageBackpackFilterMarked
                                : SH.ViewPageBackpackFilterUnmarked;
                            matches.Add(tokens.Contains(markState));
                        }
                    }

                    break;

                case SearchTokenKind.BackpackFoodQuality:
                    if (foodQualityMap.TryGetValue(item.Entity.ItemId, out int qualityIndex))
                    {
                        string qualityName = qualityIndex switch
                        {
                            0 => SH.ViewPageBackpackFilterFoodQualitySuspicious,
                            1 => SH.ViewPageBackpackFilterFoodQualityNormal,
                            2 => SH.ViewPageBackpackFilterFoodQualityDelicious,
                            _ => string.Empty,
                        };
                        matches.Add(tokens.Contains(qualityName));
                    }

                    break;

                case SearchTokenKind.BackpackCookFoodType:
                    if (foodTypeMap.TryGetValue(item.Entity.ItemId, out CookFoodType foodType))
                    {
                        string foodTypeName = foodType.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)!;
                        matches.Add(tokens.Contains(foodTypeName));
                    }

                    break;

                case SearchTokenKind.BackpackReliquarySet:
                    if (item is BackpackReliquaryItemView rSet)
                    {
                        matches.Add(tokens.Contains(rSet.SetName ?? string.Empty));
                    }

                    break;

                case SearchTokenKind.BackpackEquipType:
                    if (item is BackpackReliquaryItemView rEquip)
                    {
                        matches.Add(tokens.Contains(rEquip.EquipTypeName));
                    }

                    break;

                default:
                    matches.Add(false);
                    break;
            }
        }

        return matches.Count > 0 && matches.All(r => r);
    }
}
