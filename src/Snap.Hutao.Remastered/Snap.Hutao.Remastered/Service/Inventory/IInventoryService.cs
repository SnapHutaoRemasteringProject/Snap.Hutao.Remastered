// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.Cultivation;
using Snap.Hutao.Remastered.ViewModel.Cultivation;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Inventory;

internal interface IInventoryService
{
    ImmutableArray<InventoryItemView> GetInventoryItemViews(ICultivationMetadataContext context, CultivateProject cultivateProject, ICommand saveCommand);

    void RemoveInventoryItems(CultivateProject cultivateProject);

    void SaveInventoryItem(InventoryItemView item);

    ValueTask RefreshInventoryAsync(RefreshOptions refreshOptions);
}