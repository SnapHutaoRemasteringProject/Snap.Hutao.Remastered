// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.UI.Xaml.Data.Converter;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;

namespace Snap.Hutao.Remastered.Model.Metadata.Converter;

public sealed partial class CookFoodTypeIconConverter : ValueConverter<CookFoodType, Uri>
{
    public static Uri CookFoodTypeToIconUri(CookFoodType type)
    {
        string filename = type switch
        {
            CookFoodType.COOK_FOOD_ATTACK => "UI_Talent_Cook_Attack.png",
            CookFoodType.COOK_FOOD_DEFENSE => "UI_Talent_Cook_Defense.png",
            CookFoodType.COOK_FOOD_FUNCTION => "UI_Talent_Cook_ExtraFailedItem.png",
            CookFoodType.COOK_FOOD_HEAL => "UI_Talent_Cook_Heal.png",
            _ => throw HutaoException.NotSupported(),
        };

        return StaticResourcesEndpoints.StaticRaw("Talent", filename).ToUri();
    }

    public override Uri Convert(CookFoodType from)
    {
        return CookFoodTypeToIconUri(from);
    }
}
