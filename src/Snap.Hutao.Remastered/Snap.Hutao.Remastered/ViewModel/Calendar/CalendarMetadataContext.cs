// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Service.Cultivation;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableArray;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Calendar;

internal sealed class CalendarMetadataContext : CultivationMetadataContext, IMetadataContext,
    ICultivationMetadataContext,
    IMetadataArrayAvatarSource,
    IMetadataArrayWeaponSource,
    IMetadataDictionaryIdMaterialSource
{
    public ImmutableArray<Avatar> Avatars { get; set; }

    public ImmutableArray<Weapon> Weapons { get; set; }
}