// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.UI.Xaml.Data.Converter;

namespace Snap.Hutao.Remastered.Model.Metadata.Converter;

public sealed partial class EquipTypeIconConverter : ValueConverter<EquipType, Uri>
{
    public static Uri EquipTypeToIconUri(EquipType type)
    {
        string filename = type switch
        {
            EquipType.EQUIP_BRACER => "UI_Icon_RelicType1.png",
            EquipType.EQUIP_NECKLACE => "UI_Icon_RelicType2.png",
            EquipType.EQUIP_SHOES => "UI_Icon_RelicType3.png",
            EquipType.EQUIP_RING => "UI_Icon_RelicType4.png",
            EquipType.EQUIP_DRESS => "UI_Icon_RelicType5.png",
            _ => throw HutaoException.NotSupported(),
        };

        return InstalledLocation.ToAbsoluteUri($"ms-appx:///Resource/Icon/{filename}");
    }

    public override Uri Convert(EquipType from)
    {
        return EquipTypeToIconUri(from);
    }
}
