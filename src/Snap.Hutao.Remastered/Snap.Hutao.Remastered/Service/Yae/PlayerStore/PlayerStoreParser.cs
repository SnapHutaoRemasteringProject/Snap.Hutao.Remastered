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
        using (CodedInputStream stream = bytes.CreateCodedInput())
        {
            try
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
    }

    public static ImmutableArray<BackpackItem> ParseToBackpackItems(ByteString bytes, Guid archiveId)
    {
        ImmutableArray<BackpackItem>.Builder builder = ImmutableArray.CreateBuilder<BackpackItem>();
        using (CodedInputStream stream = bytes.CreateCodedInput())
        {
            try
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
                                        Item item = Item.Parser.ParseFrom(inputStream);
                                        BackpackItem? backpackItem = ConvertToBackpackItem(item, archiveId);
                                        if (backpackItem is not null)
                                        {
                                            builder.Add(backpackItem);
                                        }
                                    }
                                }

                                break;
                            }
                    }
                }
            }
            catch (InvalidProtocolBufferException)
            {
                // Partial read is acceptable - return what we have
            }
        }

        return builder.ToImmutable();
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
            IsLocked = equip.IsLocked,
            IsMarked = reliquary.IsMarked,
        };
    }
}