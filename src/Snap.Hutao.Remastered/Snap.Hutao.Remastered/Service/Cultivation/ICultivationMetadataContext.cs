// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableArray;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;

namespace Snap.Hutao.Remastered.Service.Cultivation;

internal interface ICultivationMetadataContext : IMetadataContext,
    IMetadataArrayMaterialSource,
    IMetadataDictionaryIdMaterialSource,
    IMetadataDictionaryIdAvatarSource,
    IMetadataDictionaryIdWeaponSource,
    IMetadataDictionaryResultMaterialIdCombineSource
{
    Item GetAvatarItem(AvatarId avatarId);

    Item GetWeaponItem(WeaponId weaponId);
}