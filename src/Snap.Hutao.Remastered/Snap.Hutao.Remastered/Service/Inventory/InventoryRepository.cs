// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Immutable;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.Inventory;

[Service(ServiceLifetime.Singleton, typeof(IInventoryRepository))]
public sealed partial class InventoryRepository : IInventoryRepository
{
    [GeneratedConstructor]
    public partial InventoryRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public void RemoveInventoryItemRangeByProjectId(Guid projectId)
    {
        this.WithDatabase(context => AppDbQueries.DeleteInventoryByProject(context, projectId));
    }

    public void AddInventoryItemRangeByProjectId(IEnumerable<InventoryItem> items)
    {
        this.AddRange(items);
    }

    public void UpdateInventoryItem(InventoryItem item)
    {
        this.Update(item);
    }

    public ImmutableArray<InventoryItem> GetInventoryItemImmutableArrayByProjectId(Guid projectId)
    {
        return this.WithDatabase(context => AppDbQueries.InventoryByProject(context, projectId)).ToImmutableArray();
    }

    public ImmutableDictionary<uint, InventoryItem> GetInventoryItemImmutableDictionaryByProjectId(Guid projectId)
    {
        return this.WithDatabase(context => AppDbQueries.InventoryByProject(context, projectId)).ToImmutableDictionary(i => i.ItemId);
    }
}