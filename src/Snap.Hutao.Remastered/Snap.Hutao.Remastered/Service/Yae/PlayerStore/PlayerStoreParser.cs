// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Google.Protobuf;
using Snap.Hutao.Remastered.Core.Protobuf;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.InterChange.Inventory;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Yae.PlayerStore;

public static class PlayerStoreParser
{
    public static UIIF? Parse(ByteString bytes)
    {
        List<Item> items = [];
        try
        {
            ParseItems(bytes, items);
        }
        catch (InvalidProtocolBufferException)
        {
            return default;
        }

        return new()
        {
            Info = UIIFInfo.CreateForEmbeddedYae(),
            List = [.. items.Select(UIIFItem.FromInGameItem)],
        };
    }

    /// <remarks>
    /// <paramref name="equippedItemOwners"/> maps an item guid to the id of the avatar equipping it.
    /// Pass <see langword="null"/> when the avatar data is unavailable, so the equipment state is
    /// left unknown instead of being reported as "not equipped".
    /// </remarks>
    public static ImmutableArray<BackpackItem> ParseToBackpackItems(ByteString bytes, Guid archiveId, ImmutableDictionary<ulong, uint>? equippedItemOwners)
    {
        List<Item> items = [];
        try
        {
            ParseItems(bytes, items);
        }
        catch (InvalidProtocolBufferException)
        {
            // Partial read is acceptable - return what we have
        }

        ImmutableArray<BackpackItem>.Builder builder = ImmutableArray.CreateBuilder<BackpackItem>(items.Count);
        foreach (Item item in items)
        {
            if (ConvertToBackpackItem(item, archiveId) is { } backpackItem)
            {
                if (equippedItemOwners is not null)
                {
                    backpackItem.EquippedAvatarId = equippedItemOwners.GetValueOrDefault(backpackItem.Guid);
                }

                builder.Add(backpackItem);
            }
        }

        return builder.ToImmutable();
    }

    private static void ParseItems(ByteString bytes, List<Item> items)
    {
        using (CodedInputStream stream = bytes.CreateCodedInput())
        {
            while (stream.TryReadTag(out uint tag))
            {
                switch (WireFormat.GetTagWireType(tag))
                {
                    case WireFormat.WireType.Varint:
                        {
                            _ = stream.ReadUInt32();
                            continue;
                        }

                    case WireFormat.WireType.LengthDelimited:
                        {
                            using (CodedInputStream inputStream = stream.UnsafeReadLengthDelimitedStream())
                            {
                                while (inputStream.TryPeekTag(out _))
                                {
                                    items.Add(Item.Parser.ParseFrom(inputStream));
                                }
                            }

                            break;
                        }
                }
            }
        }
    }

    private static BackpackItem? ConvertToBackpackItem(Item item, Guid archiveId)
    {
        switch (item.DetailCase)
        {
            case Item.DetailOneofCase.Material:
                return new()
                {
                    ArchiveId = archiveId,
                    ItemId = item.ItemId,
                    Count = item.Material?.Count ?? 0,
                };

            case Item.DetailOneofCase.Equip:
                {
                    Equip? equip = item.Equip;
                    if (equip is null)
                    {
                        return null;
                    }

                    return equip.DetailCase switch
                    {
                        Equip.DetailOneofCase.Weapon => CreateWeaponItem(item, equip, archiveId),
                        Equip.DetailOneofCase.Reliquary => CreateReliquaryItem(item, equip, archiveId),
                        _ => new()
                        {
                            ArchiveId = archiveId,
                            ItemId = item.ItemId,
                            Count = 1,
                        },
                    };
                }

            case Item.DetailOneofCase.Furniture:
                return new()
                {
                    ArchiveId = archiveId,
                    ItemId = item.ItemId,
                    Count = item.Furniture?.Count ?? 0,
                };

            default:
                return new()
                {
                    ArchiveId = archiveId,
                    ItemId = item.ItemId,
                    Count = 1,
                };
        }
    }

    private static BackpackItem CreateWeaponItem(Item item, Equip equip, Guid archiveId)
    {
        Weapon? weapon = equip.Weapon;
        if (weapon is null)
        {
            return new()
            {
                ArchiveId = archiveId,
                ItemId = item.ItemId,
                Count = 1,
            };
        }

        uint refinementRank = 0;
        if (weapon.AffixMap is not null)
        {
            foreach (uint value in weapon.AffixMap.Values)
            {
                refinementRank += value;
            }
        }

        return new()
        {
            ArchiveId = archiveId,
            ItemId = item.ItemId,
            Guid = item.Guid,
            Count = 1,
            Level = weapon.Level,
            PromoteLevel = weapon.PromoteLevel,
            RefinementRank = refinementRank,
            IsLocked = equip.IsLocked,
        };
    }

    private static BackpackItem CreateReliquaryItem(Item item, Equip equip, Guid archiveId)
    {
        Reliquary? reliquary = equip.Reliquary;
        if (reliquary is null)
        {
            return new()
            {
                ArchiveId = archiveId,
                ItemId = item.ItemId,
                Count = 1,
            };
        }

        return new()
        {
            ArchiveId = archiveId,
            ItemId = item.ItemId,
            Guid = item.Guid,
            Count = 1,
            Level = reliquary.Level,
            MainPropId = reliquary.MainPropId,
            AppendPropIdListJson = reliquary.AppendPropIdList is { Count: > 0 }
                ? JsonSerializer.Serialize(reliquary.AppendPropIdList.ToArray())
                : null,
            PurchasedAppendPropIdListJson = reliquary.PurchasedAppendPropIdList is { Count: > 0 }
                ? JsonSerializer.Serialize(reliquary.PurchasedAppendPropIdList.ToArray())
                : null,
            DefiniteAppendPropIdListJson = reliquary.DefiniteAppendPropIdList is { Count: > 0 }
                ? JsonSerializer.Serialize(reliquary.DefiniteAppendPropIdList.ToArray())
                : null,
            IsLocked = equip.IsLocked,
            IsMarked = reliquary.IsMarked,
        };
    }
}