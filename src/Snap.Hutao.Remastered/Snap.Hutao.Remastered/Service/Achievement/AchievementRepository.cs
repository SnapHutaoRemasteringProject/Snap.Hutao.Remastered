// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Abstraction;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using EntityAchievement = Snap.Hutao.Remastered.Model.Entity.Achievement;

using Snap.Hutao.Remastered.Core.Database;

namespace Snap.Hutao.Remastered.Service.Achievement;

[Service(ServiceLifetime.Singleton, typeof(IAchievementRepository))]
public sealed partial class AchievementRepository : IAchievementRepository
{
    [GeneratedConstructor]
    public partial AchievementRepository(IServiceProvider serviceProvider);

    public partial IServiceProvider ServiceProvider { get; }

    public FrozenDictionary<AchievementId, EntityAchievement> GetAchievementMapByArchiveId(Guid archiveId)
    {
        try
        {
            return this.WithDatabase(context => AppDbQueries.AchievementsByArchive(context, archiveId)).ToFrozenDictionary(a => (AchievementId)a.Id);
        }
        catch (ArgumentException ex)
        {
            throw HutaoException.Throw(SH.ServiceAchievementUserdataCorruptedAchievementIdNotUnique, ex);
        }
    }

    public int GetFinishedAchievementCountByArchiveId(Guid archiveId)
    {
        return this.WithDatabase(context => AppDbQueries.FinishedAchievementCount(context, archiveId));
    }

    public ImmutableArray<EntityAchievement> GetLatestFinishedAchievementImmutableArrayByArchiveId(Guid archiveId, int take)
    {
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses.
        // Convert the values to a supported type, or use LINQ to Objects to order the results on the client side.
        return this.WithDatabase(context => AppDbQueries.LatestFinishedAchievements(context, archiveId, take)).ToImmutableArray();
    }

    public void OverwriteAchievement(EntityAchievement achievement)
    {
        this.DeleteByInnerId(achievement);
        if (achievement.Status >= AchievementStatus.STATUS_FINISHED)
        {
            this.Add(achievement);
        }
    }

    public ObservableCollection<AchievementArchive> GetAchievementArchiveCollection()
    {
        return this.ObservableCollection<AchievementArchive>();
    }

    public void RemoveAchievementArchive(AchievementArchive archive)
    {
        // Cascade delete the achievements.
        this.Delete(archive);
    }

    public ImmutableArray<EntityAchievement> GetAchievementImmutableArrayByArchiveId(Guid archiveId)
    {
        return this.WithDatabase(context => AppDbQueries.AchievementsByArchive(context, archiveId)).ToImmutableArray();
    }

    public ImmutableArray<AchievementArchive> GetAchievementArchiveImmutableArray()
    {
        return this.ImmutableArray<AchievementArchive>();
    }

    public AchievementArchive? GetAchievementArchiveById(Guid archiveId)
    {
        return this.WithDatabase(context => AppDbQueries.AchievementArchiveById(context, archiveId));
    }

    public AchievementArchive? GetAchievementArchiveByName(string name)
    {
        return this.WithDatabase(context => AppDbQueries.AchievementArchiveByName(context, name));
    }

    public void AddAchievementArchive(AchievementArchive archive)
    {
        this.Add(archive);
    }
}