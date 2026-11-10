using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.Service.Backpack;
using Snap.Hutao.Remastered.Service.Cultivation;
using Snap.Hutao.Remastered.Service.GachaLog;

internal static partial class DatabaseVerification
{
    private static void VerifyBusinessQueries(AppDbContext context, User user, AchievementArchive achievementArchive, BackpackArchive backpackArchive, GachaArchive gachaArchive, CultivateProject project, CultivateEntry entry)
    {
        Guid missing = Guid.NewGuid();
        Require(AppDbQueries.DailyNoteContainsUid(context, "100000001") && !AppDbQueries.DailyNoteContainsUid(context, "missing"), "Parameterized existence");
        Require(AppDbQueries.DailyNotesIncludingUser(context).Single().User.Aid == "test-user", "Include user navigation");
        Require(!AppDbQueries.UserContainsMid(context, "missing"), "Missing user mid");
        Require(AppDbQueries.InventoryByProject(context, project.InnerId).Single().Count == 10 && AppDbQueries.InventoryByProject(context, missing).Length == 0, "Inventory scope");
        Require(AppDbQueries.CultivateEntriesByProject(context, project.InnerId).Single().InnerId == entry.InnerId, "Cultivate project scope");
        Require(AppDbQueries.CultivateEntriesWithLevels(context, project.InnerId).Single().LevelInformation!.AvatarLevelTo == 90, "Include level navigation");
        Require(AppDbQueries.CultivateItemsByEntry(context, entry.InnerId).Single().Count == 42, "Cultivate item scope");
        Require(AppDbQueries.CultivateEntriesByProjectAndItem(context, project.InnerId, 10000001).Length == 1 && AppDbQueries.CultivateEntriesByProjectAndItem(context, project.InnerId, 1).Length == 0, "Multiple query parameters");
        Require(AppDbQueries.CultivateProjectById(context, project.InnerId)?.Name == "AOT cultivate" && AppDbQueries.CultivateProjectById(context, missing) is null, "Project identity");
        Require(AppDbQueries.SelectedCultivateProject(context) is null, "Unselected project");
        Require(AppDbQueries.CultivateProjectIdByEntry(context, entry.InnerId) == project.InnerId, "Project id differs from entry id");
        Require(AppDbQueries.ProfilePictureByUid(context, "100000001")?.AvatarId == 10000001 && AppDbQueries.ProfilePictureByUid(context, "missing") is null, "Profile identity");
        Require(AppDbQueries.AvatarStrategyById(context, 10000001)?.ChineseStrategyId == 15 && AppDbQueries.AvatarStrategyById(context, 1) is null, "Avatar parameter");
        Require(AppDbQueries.ObjectCacheByKey(context, "aot-cache") is { Value: null }, "Nullable cache value");
        Require(AppDbQueries.AchievementsByArchive(context, achievementArchive.InnerId).Length == 1 && AppDbQueries.AchievementsByArchive(context, missing).Length == 0, "Achievement scope");
        Require(AppDbQueries.FinishedAchievementCount(context, achievementArchive.InnerId) == 1, "Enum count");
        Require(AppDbQueries.LatestFinishedAchievements(context, achievementArchive.InnerId, 1).Single().Id == 123 && AppDbQueries.LatestFinishedAchievements(context, achievementArchive.InnerId, 0).Length == 0, "Date order and parameterized take");
        Require(AppDbQueries.AchievementArchiveById(context, achievementArchive.InnerId)?.Name == "AOT achievement", "Archive identity");
        Require(AppDbQueries.AchievementArchiveByName(context, "AOT achievement")?.InnerId == achievementArchive.InnerId, "Archive name");
        Require(AppDbQueries.BackpackItemsByArchive(context, backpackArchive.InnerId).Single().Count == 5 && AppDbQueries.BackpackItemsByArchive(context, missing).Length == 0, "Backpack scope");
        Require(AppDbQueries.BackpackArchiveById(context, backpackArchive.InnerId)?.Name == "AOT backpack", "Backpack identity");
        Require(AppDbQueries.BackpackArchiveByName(context, "AOT backpack")?.InnerId == backpackArchive.InnerId, "Backpack name");
        Require(AppDbQueries.AvatarInfosByUid(context, "100000001").Length == 1 && AppDbQueries.AvatarInfosByUid(context, "missing").Length == 0, "Avatar uid");
        Require(AppDbQueries.AvatarScoreSettingById(context, 10000001) is not null, "Score algorithm identity");
        Require(AppDbQueries.GachaArchiveById(context, gachaArchive.InnerId)?.Uid == "100000001", "Gacha identity");
        Require(AppDbQueries.GachaArchiveByUid(context, "100000001")?.InnerId == gachaArchive.InnerId, "Gacha uid");
        Require(AppDbQueries.GachaArchiveUids(context).SequenceEqual(["100000001"]), "Scalar projection");
        Require(AppDbQueries.ActiveScoreConfig(context) is not null && AppDbQueries.OrderedScoreConfigs(context).Length == 2, "Seed and score configuration");
        Require(AppDbQueries.OtherActiveScoreConfigs(context, missing).Length == 1 && AppDbQueries.ScoreConfigById(context, missing) is null, "Config filter");
        Require(AppDbQueries.SpiralAbyssByUid(context, "100000001").Single().ScheduleId == 123, "Spiral scope/order");
        Require(AppDbQueries.RoleCombatByUid(context, "100000001").Single().ScheduleId == 124, "Role combat scope/order");
        Require(AppDbQueries.HardChallengeByUid(context, "100000001").Single().ScheduleId == 125, "Hard challenge scope/order");
        Require(AppDbQueries.GachaByArchive(context, gachaArchive.InnerId).Length == 1, "Gacha scope");
        Require(AppDbQueries.NewestGacha(context, gachaArchive.InnerId, default)?.Id == 123 && AppDbQueries.NewestGacha(context, missing, default) is null, "Newest gacha and missing");
        Require(AppDbQueries.OldestGacha(context, gachaArchive.InnerId, default)?.Id == 123 && AppDbQueries.OldestGachaInArchive(context, gachaArchive.InnerId)?.Id == 123, "Oldest gacha");
        Require(AppDbQueries.NewerGacha(context, gachaArchive.InnerId, default, 122).Length == 1 && AppDbQueries.NewerGacha(context, gachaArchive.InnerId, default, 123).Length == 0, "Strict export threshold");
        Require(AppDbQueries.BeyondGachaByArchive(context, gachaArchive.InnerId).Length == 1, "Beyond gacha scope");
        Require(AppDbQueries.NewestBeyondGacha(context, gachaArchive.InnerId, default)?.Id == 124 && AppDbQueries.OldestBeyondGacha(context, gachaArchive.InnerId, default)?.Id == 124, "Beyond extrema");
        Require(AppDbQueries.NewerBeyondGacha(context, gachaArchive.InnerId, default, 123).Length == 1 && AppDbQueries.NewerBeyondGacha(context, gachaArchive.InnerId, default, 124).Length == 0, "Beyond export threshold");
        CultivateItem item = AppDbQueries.CultivateItemsByEntry(context, entry.InnerId).Single();
        Require(AppDbQueries.UpdateCultivateItemFinished(context, item.InnerId, true) == 1 && AppDbQueries.UpdateCultivateItemFinished(context, missing, true) == 0, "Targeted update and stale row");
        Require(AppDbQueries.CultivateItemsByEntry(context, entry.InnerId).Single().IsFinished, "Finish update persistence");
        using (var transaction = context.Database.BeginTransaction())
        {
            Require(AppDbQueries.DeleteNewerGacha(context, gachaArchive.InnerId, default, 124) == 0 && AppDbQueries.DeleteNewerGacha(context, gachaArchive.InnerId, default, 123) == 1, "Inclusive deletion threshold");
            Require(AppDbQueries.DeleteNewerBeyondGacha(context, gachaArchive.InnerId, default, 124) == 1, "Inclusive beyond threshold");
            Require(AppDbQueries.DeleteInventoryByProject(context, project.InnerId) == 1, "Inventory delete");
            Require(AppDbQueries.DeleteCultivateItemsByEntry(context, entry.InnerId) == 1, "Cultivate item delete");
            Require(AppDbQueries.DeleteLevelsByEntry(context, entry.InnerId) == 1, "Level delete");
            Require(AppDbQueries.DeleteProfilePicturesByUid(context, "100000001") == 1, "Profile delete");
            Require(AppDbQueries.DeleteAvatarInfosByUid(context, "100000001") == 1, "Avatar delete");
            Require(AppDbQueries.DeleteBackpackItemsByArchive(context, backpackArchive.InnerId) == 1, "Backpack delete");
            Require(AppDbQueries.DeleteAvatarScoreSettingsByConfig(context, Guid.Empty) == 1, "Score reference delete");
            Require(AppDbQueries.DeleteByInnerId<AchievementArchive>(context, achievementArchive.InnerId) == 1 && AppDbQueries.AchievementsByArchive(context, achievementArchive.InnerId).Length == 0, "GUID and cascading archive delete");
            Require(AppDbQueries.DeleteAll<AvatarStrategy>(context) == 1 && AppDbQueries.ClearSelection<User>(context) == 1, "Finite schema bulk operations");
            transaction.Rollback();
        }
        Require(AppDbQueries.GachaByArchive(context, gachaArchive.InnerId).Length == 1, "Bulk operations participate in caller transaction");
        VerifyRepositories(context.Database.GetConnectionString()!, backpackArchive, gachaArchive, project);
        Console.WriteLine("PASS: 69 query terminals, Include, aggregates, parameters, bulk commands, rollback and real repositories.");
    }

    private static void VerifyRepositories(string connectionString, BackpackArchive archive, GachaArchive gachaArchive, CultivateProject project)
    {
        ServiceCollection services = new();
        services.AddDbContextPool<AppDbContext>(options => options.UseSqlite(connectionString).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        using ServiceProvider provider = services.BuildServiceProvider();
        BackpackRepository backpack = new(provider);
        CultivationRepository cultivation = new(provider);
        GachaLogRepository gacha = new(provider);
        Require(backpack.GetBackpackItemImmutableArrayByArchiveId(archive.InnerId).Single().Count == 5, "Real backpack repository");
        Require(cultivation.GetCultivateEntryImmutableArrayIncludingLevelInformationByProjectId(project.InnerId).Single().LevelInformation!.AvatarLevelTo == 90, "Real cultivate Include");
        Require(gacha.GetNewestGachaItemIdByArchiveIdAndQueryType(gachaArchive.InnerId, default) == 123 && gacha.GetNewestGachaItemIdByArchiveIdAndQueryType(Guid.NewGuid(), default) == 0 && gacha.GetOldestGachaItemIdByArchiveId(Guid.NewGuid()) == long.MaxValue, "Gacha missing sentinels");
        BackpackReliquaryScoreConfig config = new() { InnerId = Guid.NewGuid(), Name = "AOT repository config", IsActive = true, CritWeight = 2.5 };
        backpack.SaveReliquaryScoreConfig(config);
        Require(backpack.GetActiveReliquaryScoreConfig()?.InnerId == config.InnerId, "Config activation");
        config.CritWeight = 3.5;
        backpack.SaveReliquaryScoreConfig(config);
        Require(backpack.GetAllReliquaryScoreConfigs().Single(value => value.InnerId == config.InnerId).CritWeight == 3.5, "Tracked config update");
        backpack.DeleteReliquaryScoreConfigById(config.InnerId);
        Require(backpack.GetAllReliquaryScoreConfigs().All(value => value.InnerId != config.InnerId), "Transactional config delete");
        CultivateProject disposable = new() { Name = "AOT disposable" };
        cultivation.AddCultivateProject(disposable);
        Require(disposable.InnerId != Guid.Empty, "Generic insertion generates key");
        cultivation.RemoveCultivateProjectById(disposable.InnerId);
        Require(cultivation.GetCultivateProjectById(disposable.InnerId) is null, "Generic deletion by GUID");
    }
}
