// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Wiki;

internal sealed class CookBonusView
{
    public Material OriginItem { get; set; } = default!;

    public Material Item { get; set; } = default!;

    public static CookBonusView? Create(CookBonus? cookBonus, ImmutableDictionary<MaterialId, Material> idMaterialMap)
    {
        if (cookBonus is null)
        {
            return null;
        }

        CookBonusView view = new()
        {
            OriginItem = idMaterialMap[cookBonus.OriginItemId],
            Item = idMaterialMap[cookBonus.ItemId],
        };

        return view;
    }
}