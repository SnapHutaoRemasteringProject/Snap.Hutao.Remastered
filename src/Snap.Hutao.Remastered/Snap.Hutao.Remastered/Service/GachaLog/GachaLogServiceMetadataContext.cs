// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Abstraction;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableArray;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.GachaLog;

public sealed class GachaLogServiceMetadataContext : IMetadataContext,
    IMetadataArrayGachaEventSource,
    IMetadataDictionaryIdAvatarSource,
    IMetadataDictionaryIdWeaponSource,
    IMetadataDictionaryNameAvatarSource,
    IMetadataDictionaryNameWeaponSource,
    IMetadataDictionaryIdBeyondItemSource
{
    public ImmutableArray<GachaEvent> GachaEvents { get; set; } = default!;

    public ImmutableDictionary<AvatarId, Avatar> IdAvatarMap { get; set; } = default!;

    public ImmutableDictionary<WeaponId, Weapon> IdWeaponMap { get; set; } = default!;

    public ImmutableDictionary<string, Avatar> NameAvatarMap { get; set; } = default!;

    public ImmutableDictionary<string, Weapon> NameWeaponMap { get; set; } = default!;
    public ImmutableDictionary<BeyondItemId, BeyondItem> IdBeyondItemMap { get; set; } = default!;

    public Item GetItemByNameAndType(string name, string type)
    {
        if (string.Equals(type, SH.ModelInterchangeUIGFItemTypeAvatar, StringComparison.Ordinal))
        {
            if (NameAvatarMap.TryGetValue(name, out Avatar? avatar))
            {
                return avatar.GetOrCreateItem();
            }

            throw HutaoException.Throw(SH.FormatServiceGachaLogItemMetadataNotFound(name));
        }

        if (string.Equals(type, SH.ModelInterchangeUIGFItemTypeWeapon, StringComparison.Ordinal))
        {
            if (NameWeaponMap.TryGetValue(name, out Weapon? weapon))
            {
                return weapon.GetOrCreateItem();
            }

            throw HutaoException.Throw(SH.FormatServiceGachaLogItemMetadataNotFound(name));
        }

        throw HutaoException.NotSupported($"Unsupported item type and name: '{type},{name}'");
    }

    public uint GetItemId(GachaLogItem item)
    {
        return GetItemByNameAndType(item.Name, item.ItemType).Id;
    }

    public INameQualityAccess GetNameQualityByItemId(uint id)
    {
        uint place = id.StringLength;
        return place switch
        {
            8U => IdAvatarMap[id],
            5U => IdWeaponMap[id],
            _ => throw HutaoException.NotSupported($"Id places: {place}"),
        };
    }
}
