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
        if (!TryParseItems(bytes, out List<Item>? items))
        {
            return default;
        }

        return new()
        {
            Info = UIIFInfo.CreateForEmbeddedYae(),
            List = [.. items.Select(UIIFItem.FromInGameItem)],
        };
    }

    // Returns false when the packet could not be read as a complete item list. Callers that persist
    // the result must treat false as "keep what is already stored": a truncated or schema-shifted
    // packet yields a partial list, and storing it would drop the items that were never reached.
    public static bool TryParseToBackpackItems(ByteString bytes, Guid archiveId, ImmutableDictionary<ulong, uint> equippedItemOwners, out ImmutableArray<BackpackItem> backpackItems)
    {
        backpackItems = default;

        if (!TryParseItems(bytes, out List<Item>? items))
        {
            return false;
        }

        // A real bag always has entries, so an empty result means the packet layout changed and
        // nothing matched, not that the player owns nothing.
        if (items.Count is 0)
        {
            return false;
        }

        ImmutableArray<BackpackItem>.Builder builder = ImmutableArray.CreateBuilder<BackpackItem>(items.Count);
        foreach (Item item in items)
        {
            if (ConvertToBackpackItem(item, archiveId) is { } backpackItem)
            {
                backpackItem.EquippedAvatarId = equippedItemOwners.GetValueOrDefault(backpackItem.Guid);
                builder.Add(backpackItem);
            }
        }

        backpackItems = builder.ToImmutable();
        return true;
    }

    private static bool TryParseItems(ByteString bytes, [NotNullWhen(true)] out List<Item>? items)
    {
        List<Item> parsed = [];
        try
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
                                        parsed.Add(Item.Parser.ParseFrom(inputStream));
                                    }
                                }

                                break;
                            }
                    }
                }
            }
        }
        catch (InvalidProtocolBufferException)
        {
            items = default;
            return false;
        }

        items = parsed;
        return true;
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