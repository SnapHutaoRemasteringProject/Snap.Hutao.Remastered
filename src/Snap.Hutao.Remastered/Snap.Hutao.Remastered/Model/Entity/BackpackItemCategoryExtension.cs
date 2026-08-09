// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using System.Collections.Frozen;

namespace Snap.Hutao.Remastered.Model.Entity;

public static class BackpackItemCategoryExtension
{
    private static readonly FrozenSet<MaterialType> FoodTypes = FrozenSet.ToFrozenSet((MaterialType[])
    [
        MaterialType.MATERIAL_FOOD,
        MaterialType.MATERIAL_SPICE_FOOD,
    ]);

    private static readonly FrozenSet<MaterialType> UpgradeTypes = FrozenSet.ToFrozenSet((MaterialType[])
    [
        MaterialType.MATERIAL_AVATAR_MATERIAL,
        MaterialType.MATERIAL_WEAPON_EXP_STONE,
        MaterialType.MATERIAL_TALENT,
        MaterialType.MATERIAL_EXP_FRUIT,
        MaterialType.MATERIAL_AVATAR,
        MaterialType.MATERIAL_RELIQUARY_MATERIAL,
        MaterialType.MATERIAL_RARE_GROWTH_MATERIAL,
        MaterialType.MATERIAL_AVATAR_TALENT_MATERIAL,
    ]);

    private static readonly FrozenSet<MaterialType> GadgetTypes = FrozenSet.ToFrozenSet((MaterialType[])
    [
        MaterialType.MATERIAL_WIDGET,
        MaterialType.MATERIAL_FISH_ROD,
        MaterialType.MATERIAL_FISH_BAIT,
        MaterialType.MATERIAL_FLYCLOAK,
        MaterialType.MATERIAL_BGM,
        MaterialType.MATERIAL_PHOTOGRAPH_POSE,
        MaterialType.MATERIAL_CHANNELLER_SLAB_BUFF,
    ]);

    private static readonly FrozenSet<MaterialType> QuestTypes = FrozenSet.ToFrozenSet((MaterialType[])
    [
        MaterialType.MATERIAL_QUEST,
        MaterialType.MATERIAL_QUEST_EVENT_BOOK,
        MaterialType.MATERIAL_QUEST_ALBUM,
        MaterialType.MATERIAL_ARANARA,
        MaterialType.MATERIAL_DESHRET_MANUAL,
    ]);

    private static readonly FrozenSet<MaterialType> PreciousTypes = FrozenSet.ToFrozenSet((MaterialType[])
    [
        MaterialType.MATERIAL_CONSUME,
        MaterialType.MATERIAL_ADSORBATE,
        MaterialType.MATERIAL_ELEM_CRYSTAL,
        MaterialType.MATERIAL_CHEST,
        MaterialType.MATERIAL_CHEST_BATCH_USE,
        MaterialType.MATERIAL_CONSUME_BATCH_USE,
        MaterialType.MATERIAL_BGM,
        MaterialType.MATERIAL_RENAME_ITEM,
    ]);

    public static BackpackItemCategory GetCategory(this Material? material, uint itemId)
    {
        if (IsPreciousItemId(itemId))
        {
            return BackpackItemCategory.PreciousItem;
        }

        if (IsFoodItemId(itemId))
        {
            return BackpackItemCategory.Food;
        }

        if (IsMaterialItemId(itemId) && material != null)
        {
            return BackpackItemCategory.Material;
        }

        if (IsGadgetItemId(itemId))
        {
            return BackpackItemCategory.Gadget;
        }

        if (material is not null)
        {
            return GetMaterialCategory(material.MaterialType);
        }

        return BackpackItemCategory.Unknown;
    }

    private static bool IsFoodItemId(uint itemId)
    {
        return itemId is >= 108000 and < 109000;
    }

    private static bool IsMaterialItemId(uint itemId)
    {
        return (itemId is >= 101001 and <= 101500) ||
               (itemId is >= 130001 and <= 141000);
    }

    private static bool IsPreciousItemId(uint itemId)
    {
        return (itemId is >= 201 and <= 207) ||
               (itemId is >= 141001 and <= 200000) ||
               (itemId == 220007);
    }

    private static bool IsGadgetItemId(uint itemId)
    {
        return itemId is >= 220000 and <= 221000;
    }

    private static BackpackItemCategory GetMaterialCategory(MaterialType type)
    {
        if (UpgradeTypes.Contains(type))
        {
            return BackpackItemCategory.UpgradeItem;
        }

        if (QuestTypes.Contains(type))
        {
            return BackpackItemCategory.Quest;
        }

        if (PreciousTypes.Contains(type))
        {
            return BackpackItemCategory.PreciousItem;
        }

        if (FoodTypes.Contains(type))
        {
            return BackpackItemCategory.Food;
        }

        if (GadgetTypes.Contains(type))
        {
            return BackpackItemCategory.Gadget;
        }

        return BackpackItemCategory.Material;
    }
}
