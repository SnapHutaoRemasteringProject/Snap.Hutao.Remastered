// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.Core.Database.Abstraction;
using Snap.Hutao.Remastered.Model.Entity.Abstraction;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;

namespace Snap.Hutao.Remastered.Core.Database;

internal static partial class AppDbQueries
{
    // SQL identifiers are limited to this finite production schema map. Values
    // always use parameters; no caller-supplied identifier reaches command text.
    private static string Table<TEntity>() where TEntity : class
    {
        if (typeof(TEntity) == typeof(Achievement)) return "achievements";
        if (typeof(TEntity) == typeof(AchievementArchive)) return "achievement_archives";
        if (typeof(TEntity) == typeof(AvatarInfo)) return "avatar_infos";
        if (typeof(TEntity) == typeof(AvatarReliquaryScoreSetting)) return "avatar_reliquary_score_setting";
        if (typeof(TEntity) == typeof(AvatarStrategy)) return "avatar_strategies";
        if (typeof(TEntity) == typeof(BackpackArchive)) return "backpack_archives";
        if (typeof(TEntity) == typeof(BackpackItem)) return "backpack_items";
        if (typeof(TEntity) == typeof(BackpackReliquaryScoreConfig)) return "backpack_reliquary_score_config";
        if (typeof(TEntity) == typeof(BeyondGachaItem)) return "beyond_gacha_items";
        if (typeof(TEntity) == typeof(CultivateEntry)) return "cultivate_entries";
        if (typeof(TEntity) == typeof(CultivateEntryLevelInformation)) return "cultivate_entry_level_informations";
        if (typeof(TEntity) == typeof(CultivateItem)) return "cultivate_items";
        if (typeof(TEntity) == typeof(CultivateProject)) return "cultivate_projects";
        if (typeof(TEntity) == typeof(DailyNoteEntry)) return "daily_notes";
        if (typeof(TEntity) == typeof(GachaArchive)) return "gacha_archives";
        if (typeof(TEntity) == typeof(GachaItem)) return "gacha_items";
        if (typeof(TEntity) == typeof(GameAccount)) return "game_accounts";
        if (typeof(TEntity) == typeof(HardChallengeEntry)) return "hard_challenges";
        if (typeof(TEntity) == typeof(InventoryItem)) return "inventory_items";
        if (typeof(TEntity) == typeof(ObjectCacheEntry)) return "object_cache";
        if (typeof(TEntity) == typeof(RoleCombatEntry)) return "role_combats";
        if (typeof(TEntity) == typeof(SettingEntry)) return "settings";
        if (typeof(TEntity) == typeof(SpiralAbyssEntry)) return "spiral_abysses";
        if (typeof(TEntity) == typeof(UidProfilePicture)) return "uid_profile_pictures";
        if (typeof(TEntity) == typeof(User)) return "users";
        throw new NotSupportedException("Unregistered database entity: " + typeof(TEntity));
    }

    public static int DeleteAll<TEntity>(AppDbContext context) where TEntity : class
    {
        string sql = $"DELETE FROM [{Table<TEntity>()}];";
        return context.Database.ExecuteSqlRaw(sql);
    }

    public static int DeleteByInnerId<TEntity>(AppDbContext context, Guid innerId) where TEntity : class, IAppDbEntity
    {
        string sql = $"DELETE FROM [{Table<TEntity>()}] WHERE [InnerId] = {{0}};";
        return context.Database.ExecuteSqlRaw(sql, innerId);
    }

    public static int ClearSelection<TEntity>(AppDbContext context) where TEntity : class, ISelectable
    {
        string sql = $"UPDATE [{Table<TEntity>()}] SET [IsSelected] = 0;";
        return context.Database.ExecuteSqlRaw(sql);
    }

    public static int DeleteInventoryByProject(AppDbContext context, Guid projectId)
    {
        return context.Database.ExecuteSql($"DELETE FROM [inventory_items] WHERE [ProjectId] = {projectId};");
    }

    public static int DeleteCultivateItemsByEntry(AppDbContext context, Guid entryId)
    {
        return context.Database.ExecuteSql($"DELETE FROM [cultivate_items] WHERE [EntryId] = {entryId};");
    }

    public static int DeleteLevelsByEntry(AppDbContext context, Guid entryId)
    {
        return context.Database.ExecuteSql($"DELETE FROM [cultivate_entry_level_informations] WHERE [EntryId] = {entryId};");
    }

    public static int DeleteProfilePicturesByUid(AppDbContext context, string uid)
    {
        return context.Database.ExecuteSql($"DELETE FROM [uid_profile_pictures] WHERE [Uid] = {uid};");
    }

    public static int DeleteAvatarInfosByUid(AppDbContext context, string uid)
    {
        return context.Database.ExecuteSql($"DELETE FROM [avatar_infos] WHERE [Uid] = {uid};");
    }

    public static int DeleteBackpackItemsByArchive(AppDbContext context, Guid archiveId)
    {
        return context.Database.ExecuteSql($"DELETE FROM [backpack_items] WHERE [ArchiveId] = {archiveId};");
    }

    public static int DeleteAvatarScoreSettingsByConfig(AppDbContext context, Guid configId)
    {
        return context.Database.ExecuteSql($"DELETE FROM [avatar_reliquary_score_setting] WHERE [ConfigId] = {configId};");
    }

    public static int DeleteNewerGacha(AppDbContext context, Guid archiveId, GachaType queryType, long endId)
    {
        return context.Database.ExecuteSql($"DELETE FROM [gacha_items] WHERE [ArchiveId] = {archiveId} AND [QueryType] = {(int)queryType} AND [Id] >= {endId};");
    }

    public static int DeleteNewerBeyondGacha(AppDbContext context, Guid archiveId, GachaType queryType, long endId)
    {
        return context.Database.ExecuteSql($"DELETE FROM [beyond_gacha_items] WHERE [ArchiveId] = {archiveId} AND [QueryType] = {(int)queryType} AND [Id] >= {endId};");
    }

    public static int UpdateCultivateItemFinished(AppDbContext context, Guid innerId, bool isFinished)
    {
        return context.Database.ExecuteSql($"UPDATE [cultivate_items] SET [IsFinished] = {isFinished} WHERE [InnerId] = {innerId};");
    }
}
