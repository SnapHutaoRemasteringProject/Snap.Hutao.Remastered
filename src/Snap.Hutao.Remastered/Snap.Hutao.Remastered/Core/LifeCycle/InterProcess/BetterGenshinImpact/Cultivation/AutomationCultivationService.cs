// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Metadata.Monster;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Abstraction;
using Snap.Hutao.Remastered.Service.Cultivation;
using Snap.Hutao.Remastered.Service.Inventory;
using Snap.Hutao.Remastered.Service.Metadata;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Cultivation;

[Service(ServiceLifetime.Singleton, typeof(IAutomationCultivationService))]
public sealed partial class AutomationCultivationService : IAutomationCultivationService
{
    private readonly ICultivationRepository cultivationRepository;
    private readonly IInventoryRepository inventoryRepository;
    private readonly IMetadataService metadataService;

    private Dictionary<MaterialId, List<string>>? materialToMonsters;
    private readonly SemaphoreSlim materialToMonstersGate = new(1, 1);

    [GeneratedConstructor]
    public partial AutomationCultivationService(IServiceProvider serviceProvider);

    public async ValueTask<AutomationCultivationProject?> GetCurrentProjectAsync(CancellationToken token = default)
    {
        CultivateProject? project = cultivationRepository.SingleOrDefault<CultivateProject>(p => p.IsSelected);

        if (project is null)
        {
            return default;
        }

        CultivationMetadataContext context = await metadataService.GetContextAsync<CultivationMetadataContext>(token).ConfigureAwait(false);

        Dictionary<MaterialId, List<string>> materialToMonsters = await GetMaterialToMonstersAsync(token).ConfigureAwait(false);

        Guid projectId = project.InnerId;
        ImmutableArray<CultivateEntry> entries = cultivationRepository.ImmutableArray<CultivateEntry>(e => e.ProjectId == projectId);
        ImmutableArray<AutomationCultivationEntry>.Builder entriesBuilder = ImmutableArray.CreateBuilder<AutomationCultivationEntry>();
        foreach (ref readonly CultivateEntry entry in entries.AsSpan())
        {
            Guid entryId = entry.InnerId;
            ImmutableArray<CultivateItem> items = cultivationRepository.ImmutableArray<CultivateItem>(i => i.EntryId == entryId);
            ImmutableArray<AutomationCultivationItem>.Builder itemsBuilder = ImmutableArray.CreateBuilder<AutomationCultivationItem>();
            foreach (ref readonly CultivateItem item in items.AsSpan())
            {
                Material material = context.GetMaterial(item.ItemId);
                itemsBuilder.Add(new()
                {
                    ItemId = item.ItemId,
                    Name = material.Name,
                    Count = item.Count,
                    RankLevel = (uint)material.RankLevel,
                    Monsters = materialToMonsters.TryGetValue(item.ItemId, out List<string>? names) ? names.ToImmutableArray() : ImmutableArray<string>.Empty,
                });
            }

            entriesBuilder.Add(new()
            {
                ItemId = entry.Id,
                Items = itemsBuilder.ToImmutable(),
            });
        }

        ImmutableArray<InventoryItem> inventoryItems = inventoryRepository.ImmutableArray(i => i.ProjectId == projectId);
        ImmutableArray<AutomationInventoryItem>.Builder inventoryItemsBuilder = ImmutableArray.CreateBuilder<AutomationInventoryItem>();
        foreach (ref readonly InventoryItem item in inventoryItems.AsSpan())
        {
            inventoryItemsBuilder.Add(new()
            {
                ItemId = item.ItemId,
                Name = context.GetMaterial(item.ItemId).Name,
                Count = item.Count,
            });
        }

        return new()
        {
            Entries = entriesBuilder.ToImmutable(),
            InventoryItems = inventoryItemsBuilder.ToImmutable(),
        };
    }

    // 材料 -> 掉落该材料的怪物名称（用于 BGI 端刷怪物材料时匹配路线）。
    private async ValueTask<Dictionary<MaterialId, List<string>>> GetMaterialToMonstersAsync(CancellationToken token)
    {
        if (materialToMonsters is not null)
        {
            return materialToMonsters;
        }

        using (await materialToMonstersGate.EnterAsync(token).ConfigureAwait(false))
        {
            if (materialToMonsters is not null)
            {
                return materialToMonsters;
            }

            ImmutableDictionary<MonsterDescribeId, Monster> monsters = await metadataService.GetDescribeIdToMonsterMapAsync(token).ConfigureAwait(false);
            Dictionary<MaterialId, List<string>> map = [];
            foreach (Monster monster in monsters.Values)
            {
                if (string.IsNullOrEmpty(monster.Name) || monster.Drops.IsDefaultOrEmpty)
                {
                    continue;
                }

                foreach (MaterialId drop in monster.Drops)
                {
                    List<string> names = map.GetValueOrDefault(drop) ?? [];
                    names.Add(monster.Name);
                    map[drop] = names;
                }
            }

            materialToMonsters = map;
            return map;
        }
    }
}