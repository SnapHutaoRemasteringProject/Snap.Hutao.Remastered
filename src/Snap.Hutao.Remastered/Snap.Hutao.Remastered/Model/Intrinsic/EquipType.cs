// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Model.Intrinsic;

[ExtendedEnum]
public enum EquipType
{
    EQUIP_NONE = 0,

    [LocalizationKey(nameof(SH.ModelIntrinsicEquipBracer))]
    EQUIP_BRACER = 1,

    [LocalizationKey(nameof(SH.ModelIntrinsicEquipNecklace))]
    EQUIP_NECKLACE = 2,

    [LocalizationKey(nameof(SH.ModelIntrinsicEquipShoes))]
    EQUIP_SHOES = 3,

    [LocalizationKey(nameof(SH.ModelIntrinsicEquipRing))]
    EQUIP_RING = 4,

    [LocalizationKey(nameof(SH.ModelIntrinsicEquipDress))]
    EQUIP_DRESS = 5,

    EQUIP_WEAPON = 6,
}
