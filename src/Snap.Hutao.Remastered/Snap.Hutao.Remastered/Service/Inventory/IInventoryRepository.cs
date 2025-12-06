// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Inventory;

internal interface IInventoryRepository : IRepository<InventoryItem>
{
    void AddInventoryItemRangeByProjectId(IEnumerable<InventoryItem> items);

    void RemoveInventoryItemRangeByProjectId(Guid projectId);

    void UpdateInventoryItem(InventoryItem item);

    ImmutableArray<InventoryItem> GetInventoryItemImmutableArrayByProjectId(Guid projectId);

    ImmutableDictionary<uint, InventoryItem> GetInventoryItemImmutableDictionaryByProjectId(Guid projectId);
}