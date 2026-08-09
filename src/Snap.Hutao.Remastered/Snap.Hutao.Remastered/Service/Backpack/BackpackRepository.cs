// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.Service.Backpack;

[Service(ServiceLifetime.Singleton, typeof(IBackpackRepository))]
public sealed partial class BackpackRepository : IBackpackRepository
{
    [GeneratedConstructor]
    public partial BackpackRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public ObservableCollection<BackpackArchive> GetBackpackArchiveCollection()
    {
        return this.ObservableCollection<BackpackArchive>();
    }

    public ImmutableArray<BackpackItem> GetBackpackItemImmutableArrayByArchiveId(Guid archiveId)
    {
        return this.ImmutableArray<BackpackItem, BackpackItem>(query => query.Where(i => i.ArchiveId == archiveId).OrderBy(i => i.ItemId));
    }

    public void AddBackpackArchive(BackpackArchive archive)
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            appDbContext.BackpackArchives.AddAndSave(archive);
        }
    }

    public void RemoveBackpackArchiveById(Guid archiveId)
    {
        this.DeleteByInnerId<BackpackArchive>(archiveId);
    }

    public BackpackArchive? GetBackpackArchiveById(Guid archiveId)
    {
        return this.SingleOrDefault<BackpackArchive>(a => a.InnerId == archiveId);
    }

    public BackpackArchive? GetBackpackArchiveByName(string name)
    {
        return this.SingleOrDefault<BackpackArchive>(a => a.Name == name);
    }

    public void AddBackpackItemRange(IEnumerable<BackpackItem> items)
    {
        this.AddRange(items);
    }

    public void RemoveBackpackItemRangeByArchiveId(Guid archiveId)
    {
        this.Delete<BackpackItem>(i => i.ArchiveId == archiveId);
    }

    public BackpackReliquaryScoreConfig? GetActiveReliquaryScoreConfig()
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return appDbContext.BackpackReliquaryScoreConfigs.FirstOrDefault(c => c.IsActive);
        }
    }

    public ImmutableArray<BackpackReliquaryScoreConfig> GetAllReliquaryScoreConfigs()
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return [.. appDbContext.BackpackReliquaryScoreConfigs.OrderBy(c => c.Name)];
        }
    }

    public void SaveReliquaryScoreConfig(BackpackReliquaryScoreConfig config)
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Load and attach all other active configs (override global NoTracking via Attach)
            List<BackpackReliquaryScoreConfig> others = appDbContext.BackpackReliquaryScoreConfigs
                .Where(c => c.IsActive && c.InnerId != config.InnerId)
                .ToList();
            foreach (BackpackReliquaryScoreConfig other in others)
            {
                appDbContext.Attach(other);
                other.IsActive = false;
            }

            BackpackReliquaryScoreConfig? existing = appDbContext.BackpackReliquaryScoreConfigs
                .FirstOrDefault(c => c.InnerId == config.InnerId);
            if (existing is not null)
            {
                appDbContext.Attach(existing);
                existing.PresetKey = config.PresetKey;
                existing.Name = config.Name;
                existing.IsActive = config.IsActive;
                existing.CritWeight = config.CritWeight;
                existing.CritHurtWeight = config.CritHurtWeight;
                existing.AttackPercentWeight = config.AttackPercentWeight;
                existing.ChargeEfficiencyWeight = config.ChargeEfficiencyWeight;
                existing.ElementalMasteryWeight = config.ElementalMasteryWeight;
                existing.HpPercentWeight = config.HpPercentWeight;
                existing.DefensePercentWeight = config.DefensePercentWeight;
            }
            else
            {
                appDbContext.BackpackReliquaryScoreConfigs.Add(config);
            }

            appDbContext.SaveChanges();
        }
    }

    public void DeleteReliquaryScoreConfigById(Guid configId)
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            BackpackReliquaryScoreConfig? config = appDbContext.BackpackReliquaryScoreConfigs.Find(configId);
            if (config is not null)
            {
                appDbContext.Attach(config);
                appDbContext.BackpackReliquaryScoreConfigs.Remove(config);
                appDbContext.SaveChanges();
            }
        }
    }
}
