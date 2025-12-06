// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Reliquary;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Hutao;

internal sealed class HutaoSpiralAbyssStatisticsMetadataContext : IMetadataContext,
    IMetadataDictionaryIdAvatarWithPlayersSource,
    IMetadataDictionaryIdWeaponSource,
    IMetadataDictionaryExtendedEquipAffixIdReliquarySetSource
{
    public ImmutableDictionary<AvatarId, Avatar> IdAvatarMap { get; set; } = default!;

    public ImmutableDictionary<WeaponId, Weapon> IdWeaponMap { get; set; } = default!;

    public ImmutableDictionary<ExtendedEquipAffixId, ReliquarySet> ExtendedIdReliquarySetMap { get; set; } = default!;
}