// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Yae;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Service.Yae.PlayerStore;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Backpack;

[Service(ServiceLifetime.Scoped, typeof(IBackpackService))]
public sealed partial class BackpackService : IBackpackService
{
    private readonly IBackpackRepository backpackRepository;
    private readonly IServiceProvider serviceProvider;

    [GeneratedConstructor]
    public partial BackpackService(IServiceProvider serviceProvider);

    private readonly AsyncLock archivesLock = new();

    private IAdvancedDbCollectionView<BackpackArchive>? archives;

    public async ValueTask<IAdvancedDbCollectionView<BackpackArchive>> GetArchiveCollectionAsync()
    {
        using (await archivesLock.LockAsync().ConfigureAwait(false))
        {
            return archives ??= backpackRepository.GetBackpackArchiveCollection()
                              .ToAdvancedDbCollectionView(serviceProvider);
        }
    }

    public BackpackArchive AddArchive(string name)
    {
        BackpackArchive archive = BackpackArchive.Create(name);
        backpackRepository.AddBackpackArchive(archive);

        archives = null;

        return archive;
    }

    public async ValueTask<bool> RefreshByEmbeddedYaeAsync(BackpackArchive archive, PlayerStoreResult storeResult)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ArgumentNullException.ThrowIfNull(storeResult.StoreBytes);
        ImmutableArray<BackpackItem> items = PlayerStoreParser.ParseToBackpackItems(storeResult.StoreBytes, archive.InnerId);

        // Add virtual currency items from player properties
        ImmutableArray<BackpackItem> virtualItems = CreateVirtualItems(archive.InnerId, storeResult.PropMap);

        backpackRepository.RemoveBackpackItemRangeByArchiveId(archive.InnerId);
        backpackRepository.AddBackpackItemRange(items);
        backpackRepository.AddBackpackItemRange(virtualItems);

        return true;
    }

    public async ValueTask RemoveArchiveAsync(BackpackArchive archive)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        backpackRepository.RemoveBackpackItemRangeByArchiveId(archive.InnerId);
        backpackRepository.RemoveBackpackArchiveById(archive.InnerId);

        archives = null;
    }

    public ImmutableArray<BackpackItem> GetBackpackItemImmutableArrayByArchiveId(Guid archiveId)
    {
        return backpackRepository.GetBackpackItemImmutableArrayByArchiveId(archiveId);
    }

    private static ImmutableArray<BackpackItem> CreateVirtualItems(Guid archiveId, Dictionary<InterestedPropType, double> propMap)
    {
        ImmutableArray<BackpackItem>.Builder builder = ImmutableArray.CreateBuilder<BackpackItem>();

        AddIfPositive(ItemIds.Primogem, propMap.GetValueOrDefault(InterestedPropType.PlayerHCoin) - propMap.GetValueOrDefault(InterestedPropType.PlayerWaitSubHCoin));
        AddIfPositive(ItemIds.Mora, propMap.GetValueOrDefault(InterestedPropType.PlayerSCoin) - propMap.GetValueOrDefault(InterestedPropType.PlayerWaitSubSCoin));
        AddIfPositive(ItemIds.GenesisCrystal, propMap.GetValueOrDefault(InterestedPropType.PlayerMCoin) - propMap.GetValueOrDefault(InterestedPropType.PlayerWaitSubMCoin));
        AddIfPositive(ItemIds.RealmCurrency, propMap.GetValueOrDefault(InterestedPropType.PlayerHomeCoin) - propMap.GetValueOrDefault(InterestedPropType.PlayerWaitSubHomeCoin));
        AddIfPositive(ItemIds.ToyMedal, propMap.GetValueOrDefault(InterestedPropType.PlayerRoleCombatCoin));
        AddIfPositive(ItemIds.RepertoireCoins, propMap.GetValueOrDefault(InterestedPropType.PlayerMusicGameBookCoin));

        return builder.ToImmutable();

        void AddIfPositive(uint itemId, double count)
        {
            if (count > 0)
            {
                builder.Add(new BackpackItem
                {
                    ArchiveId = archiveId,
                    ItemId = itemId,
                    Count = (uint)Math.Clamp(count, uint.MinValue, uint.MaxValue),
                });
            }
        }
    }

    public BackpackReliquaryScoreConfig CreatePreset(ReliquaryScoreConfigPreset preset)
    {
        return preset switch
        {
            ReliquaryScoreConfigPreset.ATKScaler => new()
            {
                PresetKey = preset,
                AttackPercentWeight = 1.0,
                ElementalMasteryWeight = 0,
                ChargeEfficiencyWeight = 0,
            },
            ReliquaryScoreConfigPreset.HPScaler => new()
            {
                PresetKey = preset,
                HpPercentWeight = 1.0,
                AttackPercentWeight = 0,
                ElementalMasteryWeight = 0,
                ChargeEfficiencyWeight = 0,
            },
            ReliquaryScoreConfigPreset.DEFScaler => new()
            {
                PresetKey = preset,
                DefensePercentWeight = 1.0,
                AttackPercentWeight = 0,
                ElementalMasteryWeight = 0,
                ChargeEfficiencyWeight = 0,
            },
            ReliquaryScoreConfigPreset.EM => new()
            {
                PresetKey = preset,
                ElementalMasteryWeight = 1.0,
                AttackPercentWeight = 0,
                ChargeEfficiencyWeight = 0,
            },
            _ => new() { PresetKey = preset },
        };
    }

    public BackpackReliquaryScoreConfig GetActiveReliquaryScoreConfig()
    {
        BackpackReliquaryScoreConfig? config = backpackRepository.GetActiveReliquaryScoreConfig();
        if (config is not null)
        {
            return config;
        }

        return CreatePreset(ReliquaryScoreConfigPreset.Default);
    }

    public ImmutableArray<BackpackReliquaryScoreConfig> GetAllReliquaryScoreConfigs()
    {
        return backpackRepository.GetAllReliquaryScoreConfigs();
    }

    public BackpackReliquaryScoreConfig SaveReliquaryScoreConfig(BackpackReliquaryScoreConfig config)
    {
        if (config.InnerId == Guid.Empty)
        {
            config.InnerId = Guid.NewGuid();
        }

        config.IsActive = true;
        backpackRepository.SaveReliquaryScoreConfig(config);
        return config;
    }

    public void DeleteReliquaryScoreConfig(Guid configId)
    {
        backpackRepository.DeleteReliquaryScoreConfigById(configId);
    }
}
