// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Entity.Database;
using System;
using System.Linq;

namespace Snap.Hutao.Remastered.Core.Database;

internal static partial class AppDbQueries
{
    public static TEntity[] All<TEntity>(AppDbContext context) where TEntity : class
    {
        if (typeof(TEntity) == typeof(SettingEntry)) return (TEntity[])(object)GetSettings(context);
        if (typeof(TEntity) == typeof(User)) return (TEntity[])(object)GetUsers(context);
        if (typeof(TEntity) == typeof(Achievement)) return (TEntity[])(object)GetAchievements(context);
        if (typeof(TEntity) == typeof(AchievementArchive)) return (TEntity[])(object)GetAchievementArchives(context);
        if (typeof(TEntity) == typeof(BackpackArchive)) return (TEntity[])(object)GetBackpackArchives(context);
        if (typeof(TEntity) == typeof(BackpackItem)) return (TEntity[])(object)GetBackpackItems(context);
        if (typeof(TEntity) == typeof(GachaItem)) return (TEntity[])(object)GetGachaItems(context);
        if (typeof(TEntity) == typeof(BeyondGachaItem)) return (TEntity[])(object)GetBeyondGachaItems(context);
        if (typeof(TEntity) == typeof(GachaArchive)) return (TEntity[])(object)GetGachaArchives(context);
        if (typeof(TEntity) == typeof(AvatarInfo)) return (TEntity[])(object)GetAvatarInfos(context);
        if (typeof(TEntity) == typeof(GameAccount)) return (TEntity[])(object)GetGameAccounts(context);
        if (typeof(TEntity) == typeof(DailyNoteEntry)) return (TEntity[])(object)GetDailyNotes(context);
        if (typeof(TEntity) == typeof(ObjectCacheEntry)) return (TEntity[])(object)GetObjectCache(context);
        if (typeof(TEntity) == typeof(CultivateProject)) return (TEntity[])(object)GetCultivateProjects(context);
        if (typeof(TEntity) == typeof(CultivateEntry)) return (TEntity[])(object)GetCultivateEntries(context);
        if (typeof(TEntity) == typeof(CultivateEntryLevelInformation)) return (TEntity[])(object)GetLevelInformation(context);
        if (typeof(TEntity) == typeof(CultivateItem)) return (TEntity[])(object)GetCultivateItems(context);
        if (typeof(TEntity) == typeof(InventoryItem)) return (TEntity[])(object)GetInventoryItems(context);
        if (typeof(TEntity) == typeof(SpiralAbyssEntry)) return (TEntity[])(object)GetSpiralAbysses(context);
        if (typeof(TEntity) == typeof(UidProfilePicture)) return (TEntity[])(object)GetUidProfilePictures(context);
        if (typeof(TEntity) == typeof(RoleCombatEntry)) return (TEntity[])(object)GetRoleCombats(context);
        if (typeof(TEntity) == typeof(AvatarStrategy)) return (TEntity[])(object)GetAvatarStrategies(context);
        if (typeof(TEntity) == typeof(HardChallengeEntry)) return (TEntity[])(object)GetHardChallenges(context);
        if (typeof(TEntity) == typeof(BackpackReliquaryScoreConfig)) return (TEntity[])(object)GetBackpackReliquaryScoreConfigs(context);
        if (typeof(TEntity) == typeof(AvatarReliquaryScoreSetting)) return (TEntity[])(object)GetAvatarReliquaryScoreSettings(context);
        throw new NotSupportedException("Unregistered database entity: " + typeof(TEntity));
    }

    public static SettingEntry[] GetSettings(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.Settings.AsNoTracking().ToArray();
    }

    public static User[] GetUsers(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.Users.AsNoTracking().ToArray();
    }

    public static Achievement[] GetAchievements(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.Achievements.AsNoTracking().ToArray();
    }

    public static AchievementArchive[] GetAchievementArchives(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.AchievementArchives.AsNoTracking().ToArray();
    }

    public static BackpackArchive[] GetBackpackArchives(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.BackpackArchives.AsNoTracking().ToArray();
    }

    public static BackpackItem[] GetBackpackItems(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.BackpackItems.AsNoTracking().ToArray();
    }

    public static GachaItem[] GetGachaItems(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.GachaItems.AsNoTracking().ToArray();
    }

    public static BeyondGachaItem[] GetBeyondGachaItems(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.BeyondGachaItems.AsNoTracking().ToArray();
    }

    public static GachaArchive[] GetGachaArchives(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.GachaArchives.AsNoTracking().ToArray();
    }

    public static AvatarInfo[] GetAvatarInfos(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.AvatarInfos.AsNoTracking().ToArray();
    }

    public static GameAccount[] GetGameAccounts(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.GameAccounts.AsNoTracking().ToArray();
    }

    public static DailyNoteEntry[] GetDailyNotes(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.DailyNotes.AsNoTracking().ToArray();
    }

    public static ObjectCacheEntry[] GetObjectCache(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.ObjectCache.AsNoTracking().ToArray();
    }

    public static CultivateProject[] GetCultivateProjects(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.CultivateProjects.AsNoTracking().ToArray();
    }

    public static CultivateEntry[] GetCultivateEntries(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.CultivateEntries.AsNoTracking().ToArray();
    }

    public static CultivateEntryLevelInformation[] GetLevelInformation(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.LevelInformation.AsNoTracking().ToArray();
    }

    public static CultivateItem[] GetCultivateItems(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.CultivateItems.AsNoTracking().ToArray();
    }

    public static InventoryItem[] GetInventoryItems(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.InventoryItems.AsNoTracking().ToArray();
    }

    public static SpiralAbyssEntry[] GetSpiralAbysses(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.SpiralAbysses.AsNoTracking().ToArray();
    }

    public static UidProfilePicture[] GetUidProfilePictures(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.UidProfilePictures.AsNoTracking().ToArray();
    }

    public static RoleCombatEntry[] GetRoleCombats(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.RoleCombats.AsNoTracking().ToArray();
    }

    public static AvatarStrategy[] GetAvatarStrategies(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.AvatarStrategies.AsNoTracking().ToArray();
    }

    public static HardChallengeEntry[] GetHardChallenges(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.HardChallenges.AsNoTracking().ToArray();
    }

    public static BackpackReliquaryScoreConfig[] GetBackpackReliquaryScoreConfigs(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.BackpackReliquaryScoreConfigs.AsNoTracking().ToArray();
    }

    public static AvatarReliquaryScoreSetting[] GetAvatarReliquaryScoreSettings(AppDbContext dbContext)
    {
        AppDbContext context = dbContext;
        return context.AvatarReliquaryScoreSettings.AsNoTracking().ToArray();
    }

    public static SettingEntry? ReadSetting(AppDbContext dbContext, string key)
    {
        AppDbContext context = dbContext;
        string lookupKey = key;
        return context.Settings.AsNoTracking().SingleOrDefault(entry => entry.Key == lookupKey);
    }

    public static int WriteSetting(AppDbContext context, string key, string? value)
    {
        return context.Database.ExecuteSql($"INSERT INTO settings (Key, Value) VALUES ({key}, {value}) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value;");
    }
}
