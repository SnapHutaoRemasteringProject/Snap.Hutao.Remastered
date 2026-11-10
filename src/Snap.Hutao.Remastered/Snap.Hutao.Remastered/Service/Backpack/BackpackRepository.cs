// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.Diagnostics;
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
        return this.WithDatabase(context => AppDbQueries.BackpackItemsByArchive(context, archiveId)).ToImmutableArray();
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
        return this.WithDatabase(context => AppDbQueries.BackpackArchiveById(context, archiveId));
    }

    public BackpackArchive? GetBackpackArchiveByName(string name)
    {
        return this.WithDatabase(context => AppDbQueries.BackpackArchiveByName(context, name));
    }

    public void ReplaceBackpackItemRangeByArchiveId(Guid archiveId, IEnumerable<BackpackItem> items)
    {
        List<BackpackItem> itemList = [.. items];
        using SentrySpanScope span = SentrySpanScope.StartOperation("db.replace_backpack", "db");
        span.SetData("db.rows", itemList.Count);

        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            using (IDbContextTransaction transaction = appDbContext.Database.BeginTransaction())
            {
                AppDbQueries.DeleteBackpackItemsByArchive(appDbContext, archiveId);

                appDbContext.BackpackItems.AddRange(itemList);
                appDbContext.SaveChanges();

                transaction.Commit();
            }
        }
    }

    public void RemoveBackpackItemRangeByArchiveId(Guid archiveId)
    {
        this.WithDatabase(context => AppDbQueries.DeleteBackpackItemsByArchive(context, archiveId));
    }

    public BackpackReliquaryScoreConfig? GetActiveReliquaryScoreConfig()
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return AppDbQueries.ActiveScoreConfig(appDbContext);
        }
    }

    public ImmutableArray<BackpackReliquaryScoreConfig> GetAllReliquaryScoreConfigs()
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return AppDbQueries.OrderedScoreConfigs(appDbContext).ToImmutableArray();
        }
    }

    public void SaveReliquaryScoreConfig(BackpackReliquaryScoreConfig config)
    {
        using SentrySpanScope span = SentrySpanScope.StartChild("db.save_score_config", config.Name);

        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Load and attach all other active configs (override global NoTracking via Attach)
            BackpackReliquaryScoreConfig[] others = AppDbQueries.OtherActiveScoreConfigs(appDbContext, config.InnerId);
            foreach (BackpackReliquaryScoreConfig other in others)
            {
                appDbContext.Attach(other);
                other.IsActive = false;
            }

            BackpackReliquaryScoreConfig? existing = AppDbQueries.ScoreConfigById(appDbContext, config.InnerId);
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
        using SentrySpanScope span = SentrySpanScope.StartChild("db.delete_score_config", $"{configId}");

        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            BackpackReliquaryScoreConfig? config = AppDbQueries.ScoreConfigById(appDbContext, configId);
            if (config is not null)
            {
                // ExecuteDelete 会立即落库，与下方的 SaveChanges 不在同一事务里，
                // 失败时会留下「配置还在但角色已丢失评分偏好」的中间状态，故用事务包住两次删除
                using (IDbContextTransaction transaction = appDbContext.Database.BeginTransaction())
                {
                    appDbContext.Attach(config);
                    appDbContext.BackpackReliquaryScoreConfigs.Remove(config);

                    // 同时清理「我的角色」页面引用此配置的评分算法设置，避免悬挂的 ConfigId
                    AppDbQueries.DeleteAvatarScoreSettingsByConfig(appDbContext, configId);

                    appDbContext.SaveChanges();
                    transaction.Commit();
                }
            }
        }
    }
}
