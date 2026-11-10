// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System;
using System.Linq;

namespace Snap.Hutao.Remastered.Core.Database;

internal static partial class AppDbQueries
{
    public static bool DailyNoteContainsUid(AppDbContext dbContext, string uidArgument)
    {
        AppDbContext context = dbContext;
        string uid = uidArgument;
        return context.DailyNotes.AsNoTracking().Any(n => n.Uid == uid);
    }

    public static DailyNoteEntry[] DailyNotesIncludingUser(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.DailyNotes.AsNoTracking().Include(n => n.User).ToArray();
    }

    public static bool UserContainsMid(AppDbContext dbContext, string midArgument)
    {
        AppDbContext context = dbContext;
        string mid = midArgument;
        return context.Users.AsNoTracking().Any(u => u.Mid == mid);
    }

    public static InventoryItem[] InventoryByProject(AppDbContext dbContext, Guid projectIdArgument)
    {
        AppDbContext context = dbContext;
        Guid projectId = projectIdArgument;
        return context.InventoryItems.AsNoTracking().Where(i => i.ProjectId == projectId).ToArray();
    }

    public static CultivateEntry[] CultivateEntriesByProject(AppDbContext dbContext, Guid projectIdArgument)
    {
        AppDbContext context = dbContext;
        Guid projectId = projectIdArgument;
        return context.CultivateEntries.AsNoTracking().Where(e => e.ProjectId == projectId).ToArray();
    }

    public static CultivateEntry[] CultivateEntriesWithLevels(AppDbContext dbContext, Guid projectIdArgument)
    {
        AppDbContext context = dbContext;
        Guid projectId = projectIdArgument;
        return context.CultivateEntries.AsNoTracking().Where(e => e.ProjectId == projectId).Include(e => e.LevelInformation).ToArray();
    }

    public static CultivateItem[] CultivateItemsByEntry(AppDbContext dbContext, Guid entryIdArgument)
    {
        AppDbContext context = dbContext;
        Guid entryId = entryIdArgument;
        return context.CultivateItems.AsNoTracking().Where(i => i.EntryId == entryId).OrderBy(i => i.ItemId).ToArray();
    }

    public static CultivateEntry[] CultivateEntriesByProjectAndItem(AppDbContext dbContext, Guid projectIdArgument, uint itemIdArgument)
    {
        AppDbContext context = dbContext;
        Guid projectId = projectIdArgument;
        uint itemId = itemIdArgument;
        return context.CultivateEntries.AsNoTracking().Where(e => e.ProjectId == projectId && e.Id == itemId).ToArray();
    }

    public static CultivateProject? CultivateProjectById(AppDbContext dbContext, Guid projectIdArgument)
    {
        AppDbContext context = dbContext;
        Guid projectId = projectIdArgument;
        return context.CultivateProjects.AsNoTracking().SingleOrDefault(p => p.InnerId == projectId);
    }

    public static CultivateProject? SelectedCultivateProject(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.CultivateProjects.AsNoTracking().SingleOrDefault(p => p.IsSelected);
    }

    public static Guid CultivateProjectIdByEntry(AppDbContext dbContext, Guid entryIdArgument)
    {
        AppDbContext context = dbContext;
        Guid entryId = entryIdArgument;
        return context.CultivateEntries.AsNoTracking().Where(e => e.InnerId == entryId).Select(e => e.ProjectId).Single();
    }

    public static UidProfilePicture? ProfilePictureByUid(AppDbContext dbContext, string uidArgument)
    {
        AppDbContext context = dbContext;
        string uid = uidArgument;
        return context.UidProfilePictures.AsNoTracking().SingleOrDefault(n => n.Uid == uid);
    }

    public static AvatarStrategy? AvatarStrategyById(AppDbContext dbContext, uint avatarIdArgument)
    {
        AppDbContext context = dbContext;
        uint avatarId = avatarIdArgument;
        return context.AvatarStrategies.AsNoTracking().SingleOrDefault(s => s.AvatarId == avatarId);
    }

    public static ObjectCacheEntry? ObjectCacheByKey(AppDbContext dbContext, string keyArgument)
    {
        AppDbContext context = dbContext;
        string key = keyArgument;
        return context.ObjectCache.AsNoTracking().SingleOrDefault(e => e.Key == key);
    }

    public static Achievement[] AchievementsByArchive(AppDbContext dbContext, Guid archiveIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        return context.Achievements.AsNoTracking().Where(a => a.ArchiveId == archiveId).ToArray();
    }

    public static int FinishedAchievementCount(AppDbContext dbContext, Guid archiveIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        return context.Achievements.AsNoTracking().Where(a => a.ArchiveId == archiveId).Count(a => (int)a.Status >= (int)AchievementStatus.STATUS_FINISHED);
    }

    public static Achievement[] LatestFinishedAchievements(AppDbContext dbContext, Guid archiveIdArgument, int takeArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        int take = takeArgument;
        return context.Achievements.AsNoTracking().Where(a => a.ArchiveId == archiveId && (int)a.Status >= (int)AchievementStatus.STATUS_FINISHED).OrderByDescending(a => a.Time.ToString()).Take(take).ToArray();
    }

    public static AchievementArchive? AchievementArchiveById(AppDbContext dbContext, Guid archiveIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        return context.AchievementArchives.AsNoTracking().SingleOrDefault(a => a.InnerId == archiveId);
    }

    public static AchievementArchive? AchievementArchiveByName(AppDbContext dbContext, string nameArgument)
    {
        AppDbContext context = dbContext;
        string name = nameArgument;
        return context.AchievementArchives.AsNoTracking().SingleOrDefault(a => a.Name == name);
    }

    public static BackpackItem[] BackpackItemsByArchive(AppDbContext dbContext, Guid archiveIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        return context.BackpackItems.AsNoTracking().Where(i => i.ArchiveId == archiveId).OrderBy(i => i.ItemId).ToArray();
    }

    public static BackpackArchive? BackpackArchiveById(AppDbContext dbContext, Guid archiveIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        return context.BackpackArchives.AsNoTracking().SingleOrDefault(a => a.InnerId == archiveId);
    }

    public static BackpackArchive? BackpackArchiveByName(AppDbContext dbContext, string nameArgument)
    {
        AppDbContext context = dbContext;
        string name = nameArgument;
        return context.BackpackArchives.AsNoTracking().SingleOrDefault(a => a.Name == name);
    }

    public static AvatarInfo[] AvatarInfosByUid(AppDbContext dbContext, string uidArgument)
    {
        AppDbContext context = dbContext;
        string uid = uidArgument;
        return context.AvatarInfos.AsNoTracking().Where(i => i.Uid == uid).ToArray();
    }

    public static AvatarReliquaryScoreSetting? AvatarScoreSettingById(AppDbContext dbContext, uint avatarIdArgument)
    {
        AppDbContext context = dbContext;
        uint avatarId = avatarIdArgument;
        return context.AvatarReliquaryScoreSettings.AsNoTracking().SingleOrDefault(s => s.AvatarId == avatarId);
    }

    public static GachaArchive? GachaArchiveById(AppDbContext dbContext, Guid archiveIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        return context.GachaArchives.AsNoTracking().SingleOrDefault(a => a.InnerId == archiveId);
    }

    public static GachaArchive? GachaArchiveByUid(AppDbContext dbContext, string uidArgument)
    {
        AppDbContext context = dbContext;
        string uid = uidArgument;
        return context.GachaArchives.AsNoTracking().SingleOrDefault(a => a.Uid == uid);
    }

    public static string[] GachaArchiveUids(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.GachaArchives.AsNoTracking().Select(a => a.Uid).ToArray();
    }

    public static BackpackReliquaryScoreConfig? ActiveScoreConfig(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.BackpackReliquaryScoreConfigs.AsNoTracking().FirstOrDefault(c => c.IsActive);
    }

    public static BackpackReliquaryScoreConfig[] OrderedScoreConfigs(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.BackpackReliquaryScoreConfigs.AsNoTracking().OrderBy(c => c.Name).ToArray();
    }

    public static BackpackReliquaryScoreConfig[] OtherActiveScoreConfigs(AppDbContext dbContext, Guid configIdArgument)
    {
        AppDbContext context = dbContext;
        Guid configId = configIdArgument;
        return context.BackpackReliquaryScoreConfigs.AsNoTracking().Where(c => c.IsActive && c.InnerId != configId).ToArray();
    }

    public static BackpackReliquaryScoreConfig? ScoreConfigById(AppDbContext dbContext, Guid configIdArgument)
    {
        AppDbContext context = dbContext;
        Guid configId = configIdArgument;
        return context.BackpackReliquaryScoreConfigs.AsNoTracking().FirstOrDefault(c => c.InnerId == configId);
    }

    public static SpiralAbyssEntry[] SpiralAbyssByUid(AppDbContext dbContext, string uidArgument)
    {
        AppDbContext context = dbContext;
        string uid = uidArgument;
        return context.SpiralAbysses.AsNoTracking().Where(s => s.Uid == uid).OrderByDescending(s => s.ScheduleId).ToArray();
    }

    public static RoleCombatEntry[] RoleCombatByUid(AppDbContext dbContext, string uidArgument)
    {
        AppDbContext context = dbContext;
        string uid = uidArgument;
        return context.RoleCombats.AsNoTracking().Where(s => s.Uid == uid).OrderByDescending(s => s.ScheduleId).ToArray();
    }

    public static HardChallengeEntry[] HardChallengeByUid(AppDbContext dbContext, string uidArgument)
    {
        AppDbContext context = dbContext;
        string uid = uidArgument;
        return context.HardChallenges.AsNoTracking().Where(s => s.Uid == uid).OrderByDescending(s => s.ScheduleId).ToArray();
    }

    public static GachaItem[] GachaByArchive(AppDbContext dbContext, Guid archiveIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        return context.GachaItems.AsNoTracking().Where(i => i.ArchiveId == archiveId).OrderBy(i => i.Id).ToArray();
    }

    public static GachaItem? NewestGacha(AppDbContext dbContext, Guid archiveIdArgument, GachaType queryTypeArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        GachaType queryType = queryTypeArgument;
        return context.GachaItems.AsNoTracking().Where(i => i.ArchiveId == archiveId && i.QueryType == queryType).OrderByDescending(i => i.Id).FirstOrDefault();
    }

    public static GachaItem? OldestGacha(AppDbContext dbContext, Guid archiveIdArgument, GachaType queryTypeArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        GachaType queryType = queryTypeArgument;
        return context.GachaItems.AsNoTracking().Where(i => i.ArchiveId == archiveId && i.QueryType == queryType).OrderBy(i => i.Id).FirstOrDefault();
    }

    public static GachaItem[] NewerGacha(AppDbContext dbContext, Guid archiveIdArgument, GachaType queryTypeArgument, long endIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        GachaType queryType = queryTypeArgument;
        long endId = endIdArgument;
        return context.GachaItems.AsNoTracking().Where(i => i.ArchiveId == archiveId && i.QueryType == queryType && i.Id > endId).OrderByDescending(i => i.Id).ToArray();
    }

    public static BeyondGachaItem[] BeyondGachaByArchive(AppDbContext dbContext, Guid archiveIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        return context.BeyondGachaItems.AsNoTracking().Where(i => i.ArchiveId == archiveId).OrderBy(i => i.Id).ToArray();
    }

    public static BeyondGachaItem? NewestBeyondGacha(AppDbContext dbContext, Guid archiveIdArgument, GachaType queryTypeArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        GachaType queryType = queryTypeArgument;
        return context.BeyondGachaItems.AsNoTracking().Where(i => i.ArchiveId == archiveId && i.QueryType == queryType).OrderByDescending(i => i.Id).FirstOrDefault();
    }

    public static BeyondGachaItem? OldestBeyondGacha(AppDbContext dbContext, Guid archiveIdArgument, GachaType queryTypeArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        GachaType queryType = queryTypeArgument;
        return context.BeyondGachaItems.AsNoTracking().Where(i => i.ArchiveId == archiveId && i.QueryType == queryType).OrderBy(i => i.Id).FirstOrDefault();
    }

    public static BeyondGachaItem[] NewerBeyondGacha(AppDbContext dbContext, Guid archiveIdArgument, GachaType queryTypeArgument, long endIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        GachaType queryType = queryTypeArgument;
        long endId = endIdArgument;
        return context.BeyondGachaItems.AsNoTracking().Where(i => i.ArchiveId == archiveId && i.QueryType == queryType && i.Id > endId).OrderByDescending(i => i.Id).ToArray();
    }

    public static GachaItem? OldestGachaInArchive(AppDbContext dbContext, Guid archiveIdArgument)
    {
        AppDbContext context = dbContext;
        Guid archiveId = archiveIdArgument;
        return context.GachaItems.AsNoTracking().Where(i => i.ArchiveId == archiveId).OrderBy(i => i.Id).FirstOrDefault();
    }
}
