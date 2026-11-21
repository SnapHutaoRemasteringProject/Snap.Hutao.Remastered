// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.Service.Abstraction;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Snap.Hutao.Remastered.Service.GachaLog;

[Service(ServiceLifetime.Singleton, typeof(IGachaLogRepository))]
public sealed partial class GachaLogRepository : IGachaLogRepository
{
    [GeneratedConstructor]
    public partial GachaLogRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public ObservableCollection<GachaArchive> GetGachaArchiveCollection()
    {
        return this.ObservableCollection<GachaArchive>();
    }

    public ImmutableArray<GachaItem> GetGachaItemImmutableArrayByArchiveId(Guid archiveId)
    {
        return this.WithDatabase(context => AppDbQueries.GachaByArchive(context, archiveId)).ToImmutableArray();
    }

    public void RemoveGachaArchiveById(Guid archiveId)
    {
        this.DeleteByInnerId<GachaArchive>(archiveId);
    }

    public long GetNewestGachaItemIdByArchiveIdAndQueryType(Guid archiveId, GachaType queryType)
    {
        GachaItem? item = this.WithDatabase(context => AppDbQueries.NewestGacha(context, archiveId, queryType));

        return item?.Id ?? 0L;
    }

    public long GetOldestGachaItemIdByArchiveId(Guid archiveId)
    {
        GachaItem? item = this.WithDatabase(context => AppDbQueries.OldestGachaInArchive(context, archiveId));

        return item?.Id ?? long.MaxValue;
    }

    public long GetOldestGachaItemIdByArchiveIdAndQueryType(Guid archiveId, GachaType queryType)
    {
        GachaItem? item = this.WithDatabase(context => AppDbQueries.OldestGacha(context, archiveId, queryType));

        return item?.Id ?? long.MaxValue;
    }

    public void AddGachaArchive(GachaArchive archive)
    {
        using (IServiceScope scope = ServiceProvider.CreateScope())
        {
            AppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            appDbContext.GachaArchives.AddAndSave(archive);
        }
    }

    [SuppressMessage("", "IDE0305")]
    public ImmutableArray<Web.Hutao.GachaLog.GachaItem> GetHutaoGachaItemListByArchiveIdAndQueryTypeNewerThanEndId(Guid archiveId, GachaType queryType, long endId)
    {
        return this.WithDatabase(context => AppDbQueries.NewerGacha(context, archiveId, queryType, endId)).Select(i => new Web.Hutao.GachaLog.GachaItem
            {
                GachaType = i.GachaType,
                QueryType = i.QueryType,
                ItemId = i.ItemId,
                Time = i.Time,
                Id = i.Id,
            }).ToImmutableArray();
    }

    public GachaArchive? GetGachaArchiveById(Guid archiveId)
    {
        return this.WithDatabase(context => AppDbQueries.GachaArchiveById(context, archiveId));
    }

    public GachaArchive? GetGachaArchiveByUid(string uid)
    {
        return this.WithDatabase(context => AppDbQueries.GachaArchiveByUid(context, uid));
    }

    public void AddGachaItemRange(IEnumerable<GachaItem> items)
    {
        this.AddRange(items);
    }

    public void RemoveGachaItemRangeByArchiveIdAndQueryTypeNewerThanEndId(Guid archiveId, GachaType queryType, long endId)
    {
        this.WithDatabase(context => AppDbQueries.DeleteNewerGacha(context, archiveId, queryType, endId));
    }

    public ImmutableArray<string> GetGachaArchiveUidImmutableArray()
    {
        return this.WithDatabase(context => AppDbQueries.GachaArchiveUids(context)).ToImmutableArray();
    }

    public void AddBeyondGachaItemRange(IEnumerable<BeyondGachaItem> items)
    {
        this.AddRange(items);
    }

    public void RemoveBeyondGachaItemRangeByArchiveIdAndGachaTypeNewerThanEndId(Guid archiveId, GachaType queryType, long endId)
    {
        this.WithDatabase(context => AppDbQueries.DeleteNewerBeyondGacha(context, archiveId, queryType, endId));
    }

    public ImmutableArray<BeyondGachaItem> GetBeyondGachaItemImmutableArrayByArchiveId(Guid archiveId)
    {
        return this.WithDatabase(context => AppDbQueries.BeyondGachaByArchive(context, archiveId)).ToImmutableArray();
    }

    public long GetNewestBeyondGachaItemIdByArchiveIdAndGachaType(Guid archiveId, GachaType queryType)
    {
        BeyondGachaItem? item = this.WithDatabase(context => AppDbQueries.NewestBeyondGacha(context, archiveId, queryType));

        return item?.Id ?? 0L;
    }

    public long GetOldestBeyondGachaItemIdByArchiveIdAndGachaType(Guid archiveId, GachaType queryType)
    {
        BeyondGachaItem? item = this.WithDatabase(context => AppDbQueries.OldestBeyondGacha(context, archiveId, queryType));

        return item?.Id ?? long.MaxValue;
    }

    [SuppressMessage("", "IDE0305")]
    public ImmutableArray<Web.Hutao.GachaLog.GachaItem> GetHutaoBeyondGachaItemListByArchiveIdAndQueryTypeNewerThanEndId(Guid archiveId, GachaType queryType, long endId)
    {
        return this.WithDatabase(context => AppDbQueries.NewerBeyondGacha(context, archiveId, queryType, endId)).Select(i => new Web.Hutao.GachaLog.GachaItem
            {
                GachaType = i.GachaType,
                QueryType = i.QueryType,
                ItemId = i.ItemId,
                Time = i.Time,
                Id = i.Id,
            }).ToImmutableArray();
    }
}
