// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Snap.Hutao.Remastered.Service.Metadata;

internal static class BydMaterialIconRecovery
{
    // BeyondCostumeDrawingExcelConfigData links these drawings to the costume at id - 10000.
    private static ReadOnlySpan<uint> DrawingsWithKnownCostume =>
    [
        270255, 270256, 270257, 270258, 270259, 270260, 270261, 270262,
        270263, 270264, 270265, 270266, 270280, 270281, 270282, 270283,
        270997, 270998, 271001, 271002, 271003, 271004, 271005, 271006,
        271007, 271008, 271013, 271014, 271017, 271018, 271775, 271776,
        271777, 271778, 271779, 271780, 271781, 271782, 271783, 271784,
        271785, 271786, 271799, 271800, 271801, 271802, 274178, 274179,
        274180, 274181, 274237, 274238, 274239, 274240, 274258, 274259,
        274260, 274261, 274300, 274301, 274302, 274303, 274304, 274305,
        274306, 274307, 275384, 275386, 275392,
    ];

    public static ImmutableDictionary<BydMaterialId, BydMaterial> RecoverMissingIcons(ImmutableDictionary<BydMaterialId, BydMaterial> materials)
    {
        ImmutableDictionary<BydMaterialId, BydMaterial>.Builder? updated = null;
        foreach ((BydMaterialId id, BydMaterial material) in materials)
        {
            if (!string.IsNullOrWhiteSpace(material.Icon))
            {
                continue;
            }

            uint itemId = (uint)id;
            string? icon = GetEmojiIcon(itemId);
            if (icon is null && DrawingsWithKnownCostume.Contains(itemId)
                && materials.TryGetValue((BydMaterialId)(itemId - 10000), out BydMaterial? costume))
            {
                icon = costume.Icon;
            }

            if (string.IsNullOrWhiteSpace(icon))
            {
                Trace.TraceWarning("Beyond item {0} has no icon in metadata.", itemId);
                continue;
            }

            (updated ??= materials.ToBuilder())[id] = material.WithIcon(icon);
        }

        return updated?.ToImmutable() ?? materials;
    }

    // The material's GAIN_EMOJI reference maps to the BeyondEmoji prefab,
    // whose published static texture uses the UI_UGC_Emoji prefix.
    private static string? GetEmojiIcon(uint itemId) => itemId switch
    {
        250459 => "UI_UGC_Emoji_Rare_14",
        250460 => "UI_UGC_Emoji_SuperRare_18",
        250461 => "UI_UGC_Emoji_SuperRare_19",
        250462 => "UI_UGC_Emoji_S0100_Boy_01",
        250463 => "UI_UGC_Emoji_S0100_Girl_01",
        250464 => "UI_UGC_Emoji_Rare_15",
        250465 => "UI_UGC_Emoji_SuperRare_20",
        250466 => "UI_UGC_Emoji_SuperRare_21",
        250467 => "UI_UGC_Emoji_S0057_Boy_01",
        250468 => "UI_UGC_Emoji_S0057_Girl_01",
        250469 => "UI_UGC_Emoji_Rare_16",
        250470 => "UI_UGC_Emoji_SuperRare_22",
        250471 => "UI_UGC_Emoji_S0122_Boy_01",
        250472 => "UI_UGC_Emoji_S0122_Girl_01",
        250473 => "UI_UGC_Emoji_Rare_17",
        250474 => "UI_UGC_Emoji_SuperRare_23",
        250475 => "UI_UGC_Emoji_S0017_Boy_01",
        250476 => "UI_UGC_Emoji_S0017_Girl_01",
        250477 => "UI_UGC_Emoji_Rare_18",
        250478 => "UI_UGC_Emoji_SuperRare_24",
        250479 => "UI_UGC_Emoji_S0166_Boy_01",
        250480 => "UI_UGC_Emoji_S0166_Girl_01",
        250481 => "UI_UGC_Emoji_Rare_19",
        250482 => "UI_UGC_Emoji_SuperRare_25",
        250483 => "UI_UGC_Emoji_SuperRare_26",
        250484 => "UI_UGC_Emoji_SuperRare_27",
        250485 => "UI_UGC_Emoji_SuperRare_28",
        250486 => "UI_UGC_Emoji_SuperRare_29",
        250487 => "UI_UGC_Emoji_S0003_Boy_01",
        250488 => "UI_UGC_Emoji_S0003_Girl_01",
        250489 => "UI_UGC_Emoji_Rare_20",
        250490 => "UI_UGC_Emoji_SuperRare_30",
        250491 => "UI_UGC_Emoji_SuperRare_31",
        250492 => "UI_UGC_Emoji_SuperRare_32",
        250493 => "UI_UGC_Emoji_S0126_Boy_01",
        250494 => "UI_UGC_Emoji_S0126_Girl_01",
        _ => null,
    };
}
