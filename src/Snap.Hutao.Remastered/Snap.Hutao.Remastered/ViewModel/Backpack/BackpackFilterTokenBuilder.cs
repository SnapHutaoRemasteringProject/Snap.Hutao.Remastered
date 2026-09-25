// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Intrinsic.Frozen;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.UI.Xaml.Control.AutoSuggestBox;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

public static class BackpackFilterTokenBuilder
{
    private static readonly Uri LockedIconUri = new("ms-appx:///Resource/Icon/UI_Icon_Locked.png");
    private static readonly Uri UnlockedIconUri = new("ms-appx:///Resource/Icon/UI_Icon_Unlock.png");
    private static readonly Uri MarkIconUri = new("ms-appx:///Resource/Icon/UI_Icon_UGC_Collect.png");
    private static readonly Uri EquippedAvatarIconUri = new("ms-appx:///Resource/Icon/UI_AvatarIcon_Side_Hutao.png");
    private static readonly Uri UnequippedAvatarIconUri = new("ms-appx:///Resource/Icon/UI_Icon_Paimon_Unequipped.png");
    private static readonly Uri PurchasedAppendPropIconUri = new("ms-appx:///Resource/Icon/UI_ItemIcon_105005.png");
    private static readonly Uri SuspiciousFoodIconUri = new("ms-appx:///Resource/Icon/Icon_Common_Cook.png");
    private static readonly Uri NormalFoodIconUri = new("ms-appx:///Resource/Icon/Icon_Good_Cook.png");
    private static readonly Uri DeliciousFoodIconUri = new("ms-appx:///Resource/Icon/Icon_Perfect_Cook.png");

    public static FrozenDictionary<string, SearchToken> Build(BackpackItemCategory category, ImmutableArray<BackpackItemView> items)
    {
        List<KeyValuePair<string, SearchToken>> tokens = [];

        switch (category)
        {
            case BackpackItemCategory.Weapon:
                // Weapon type tokens
                tokens.AddRange(IntrinsicFrozen.WeaponTypeNameValues
                    .Where(nv => nv.Value is not WeaponType.WEAPON_NONE)
                    .Select(nv => KeyValuePair.Create(nv.Name, new SearchToken(SearchTokenKind.WeaponType, nv.Name, (int)nv.Value, iconUri: WeaponTypeIconConverter.WeaponTypeToIconUri(nv.Value)))));

                // Lock state tokens
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterLocked, new SearchToken(SearchTokenKind.BackpackLockState, SH.ViewPageBackpackFilterLocked, 0, iconUri: LockedIconUri)));
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterUnlocked, new SearchToken(SearchTokenKind.BackpackLockState, SH.ViewPageBackpackFilterUnlocked, 1, iconUri: UnlockedIconUri)));

                // Equipped state tokens
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterEquipped, new SearchToken(SearchTokenKind.BackpackEquippedState, SH.ViewPageBackpackFilterEquipped, 0, sideIconUri: EquippedAvatarIconUri)));
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterUnequipped, new SearchToken(SearchTokenKind.BackpackEquippedState, SH.ViewPageBackpackFilterUnequipped, 1, sideIconUri: UnequippedAvatarIconUri)));
                break;

            case BackpackItemCategory.Reliquary:
                foreach (EquipType equipType in Enum.GetValues<EquipType>())
                {
                    if (equipType is EquipType.EQUIP_NONE or EquipType.EQUIP_WEAPON)
                    {
                        continue;
                    }

                    string name = equipType.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture) ?? equipType.ToString();
                    tokens.Add(KeyValuePair.Create(name, new SearchToken(SearchTokenKind.BackpackEquipType, name, (int)equipType, sideIconUri: EquipTypeIconConverter.EquipTypeToIconUri(equipType))));
                }

                // Reliquary set tokens (use sideIconUri for colored version), ordered by metadata sort order
                HashSet<string> seen = [];
                foreach (BackpackReliquaryItemView reliquary in items.OfType<BackpackReliquaryItemView>().OrderBy(r => r.Reliquary.SortOrder))
                {
                    if (reliquary.SetName is { } name && reliquary.SetIconUri is { } uri && seen.Add(name))
                    {
                        tokens.Add(KeyValuePair.Create(name, new SearchToken(SearchTokenKind.BackpackReliquarySet, name, (int)reliquary.Reliquary.SortOrder, sideIconUri: uri)));
                    }
                }

                // Lock state tokens
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterLocked, new SearchToken(SearchTokenKind.BackpackLockState, SH.ViewPageBackpackFilterLocked, 0, iconUri: LockedIconUri)));
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterUnlocked, new SearchToken(SearchTokenKind.BackpackLockState, SH.ViewPageBackpackFilterUnlocked, 1, iconUri: UnlockedIconUri)));

                // Mark state tokens
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterMarked, new SearchToken(SearchTokenKind.BackpackMarkState, SH.ViewPageBackpackFilterMarked, 0, iconUri: MarkIconUri)));
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterUnmarked, new SearchToken(SearchTokenKind.BackpackMarkState, SH.ViewPageBackpackFilterUnmarked, 1, iconUri: MarkIconUri)));

                // Equipped state tokens
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterEquipped, new SearchToken(SearchTokenKind.BackpackEquippedState, SH.ViewPageBackpackFilterEquipped, 0, sideIconUri: EquippedAvatarIconUri)));
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterUnequipped, new SearchToken(SearchTokenKind.BackpackEquippedState, SH.ViewPageBackpackFilterUnequipped, 1, sideIconUri: UnequippedAvatarIconUri)));

                // Purchased append prop (Sanctifying Elixir) state tokens
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterPurchasedAppendProp, new SearchToken(SearchTokenKind.BackpackPurchasedAppendProp, SH.ViewPageBackpackFilterPurchasedAppendProp, 0, sideIconUri: PurchasedAppendPropIconUri)));
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterNotPurchasedAppendProp, new SearchToken(SearchTokenKind.BackpackPurchasedAppendProp, SH.ViewPageBackpackFilterNotPurchasedAppendProp, 1, sideIconUri: PurchasedAppendPropIconUri)));
                break;

            case BackpackItemCategory.Food:
                // Cook food type tokens
                foreach (CookFoodType foodType in Enum.GetValues<CookFoodType>())
                {
                    if (foodType is CookFoodType.COOK_FOOD_NONE or CookFoodType.COOK_RECIPE)
                    {
                        continue;
                    }

                    string name = foodType.GetLocalizedDescriptionOrDefault(SH.ResourceManager, CultureInfo.CurrentCulture)!;
                    Uri iconUri = CookFoodTypeIconConverter.CookFoodTypeToIconUri(foodType);
                    tokens.Add(KeyValuePair.Create(name, new SearchToken(SearchTokenKind.BackpackCookFoodType, name, (int)foodType, sideIconUri: iconUri)));
                }

                // Food quality tokens
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterFoodQualitySuspicious, new SearchToken(SearchTokenKind.BackpackFoodQuality, SH.ViewPageBackpackFilterFoodQualitySuspicious, 0, sideIconUri: SuspiciousFoodIconUri)));
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterFoodQualityNormal, new SearchToken(SearchTokenKind.BackpackFoodQuality, SH.ViewPageBackpackFilterFoodQualityNormal, 1, sideIconUri: NormalFoodIconUri)));
                tokens.Add(KeyValuePair.Create(SH.ViewPageBackpackFilterFoodQualityDelicious, new SearchToken(SearchTokenKind.BackpackFoodQuality, SH.ViewPageBackpackFilterFoodQualityDelicious, 2, sideIconUri: DeliciousFoodIconUri)));
                break;
        }

        // Item quality tokens (after category-specific tokens)
        tokens.AddRange(IntrinsicFrozen.ItemQualityNameValues
            .Select(nv => KeyValuePair.Create(nv.Name, new SearchToken(SearchTokenKind.BackpackQuality, nv.Name, (int)nv.Value, quality: QualityColorConverter.QualityToColor(nv.Value)))));

        return tokens.ToFrozenDictionary();
    }
}
