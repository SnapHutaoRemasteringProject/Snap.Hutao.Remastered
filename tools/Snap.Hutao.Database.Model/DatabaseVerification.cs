using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Entity.Database;
using Snap.Hutao.Remastered.Model.Entity.Database.Compiled;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Web.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.SpiralAbyss;
using System.Runtime.CompilerServices;

internal static partial class DatabaseVerification
{
    public static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "hutao-production-ef-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "test.db"), Pooling = false }.ToString();
        try
        {
            DatabaseSchema.Upgrade(connectionString);
            using AppDbContext context = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString).Options);
            Require(context.Model.GetEntityTypes().Count() == 25 && ReferenceEquals(context.Model, AppDbContextModel.Instance), "Use the real compiled 25-entity production model");
            Console.WriteLine($"Dynamic code supported: {RuntimeFeature.IsDynamicCodeSupported}; compiled: {RuntimeFeature.IsDynamicCodeCompiled}; JSON reflection: {JsonSerializer.IsReflectionEnabledByDefault}.");
#if HUTAO_NATIVEAOT
            Require(!RuntimeFeature.IsDynamicCodeSupported && !RuntimeFeature.IsDynamicCodeCompiled, "Must execute as native code");
            Require(!JsonSerializer.IsReflectionEnabledByDefault, "JSON reflection must be disabled");
#endif
            DetailedCharacter avatar = Fixture<DetailedCharacter>("avatar");
            DailyNote dailyNote = Fixture<DailyNote>("daily-note");
            SpiralAbyss spiralAbyss = Fixture<SpiralAbyss>("spiral-abyss");
            RoleCombatData roleCombat = Fixture<RoleCombatData>("role-combat");
            HardChallengeData hardChallenge = Fixture<HardChallengeData>("hard-challenge");
            User user = new() { Aid = "test-user", SToken = Cookie.Parse("stoken=test-token;stuid=123"), Index = 9 };
            AchievementArchive achievementArchive = AchievementArchive.Create("AOT achievement");
            BackpackArchive backpackArchive = BackpackArchive.Create("AOT backpack");
            GachaArchive gachaArchive = GachaArchive.Create("100000001");
            CultivateProject project = new() { Name = "AOT cultivate", ServerTimeZoneOffset = TimeSpan.FromHours(8) };
            context.AddRange(user, achievementArchive, backpackArchive, gachaArchive, project);
            Require(context.SaveChanges() == 5, "Insert archive roots");
            CultivateEntry entry = new() { ProjectId = project.InnerId, Id = 10000001 };
            context.Add(entry);
            context.SaveChanges();
            object[] rows = [
                new SettingEntry("aot-setting", "initial"),
                new Achievement { ArchiveId = achievementArchive.InnerId, Id = 123, Current = 1, Status = AchievementStatus.STATUS_FINISHED },
                new BackpackItem { ArchiveId = backpackArchive.InnerId, ItemId = 123, Count = 5, EquippedAvatarId = null },
                new GachaItem { ArchiveId = gachaArchive.InnerId, Id = 123 },
                new BeyondGachaItem { ArchiveId = gachaArchive.InnerId, Id = 124 },
                new AvatarInfo { Uid = "100000001", Info2 = avatar },
                new GameAccount { Name = "AOT", MihoyoSDK = "test-sdk", Mid = null, Index = 2 },
                new DailyNoteEntry { UserId = user.InnerId, Uid = "100000001", DailyNote = dailyNote, ResinNotifyThreshold = 100 },
                new ObjectCacheEntry { Key = "aot-cache", Value = null, ExpireTime = DateTimeOffset.UnixEpoch },
                new CultivateEntryLevelInformation { EntryId = entry.InnerId, AvatarLevelTo = 90 },
                new CultivateItem { EntryId = entry.InnerId, ItemId = 123, Count = 42 },
                new InventoryItem { ProjectId = project.InnerId, ItemId = 124, Count = 10 },
                new SpiralAbyssEntry { Uid = "100000001", ScheduleId = 123, SpiralAbyss = spiralAbyss },
                new UidProfilePicture { Uid = "100000001", AvatarId = 10000001 },
                new RoleCombatEntry { Uid = "100000001", ScheduleId = 124, RoleCombatData = roleCombat },
                new AvatarStrategy { AvatarId = 10000001, ChineseStrategyId = 15 },
                new HardChallengeEntry { Uid = "100000001", ScheduleId = 125, HardChallengeData = hardChallenge },
                new BackpackReliquaryScoreConfig { InnerId = Guid.NewGuid(), Name = "AOT custom score", CritWeight = 1.5 },
                new AvatarReliquaryScoreSetting { AvatarId = 10000001 }
            ];
            context.AddRange(rows);
            Require(context.SaveChanges() == rows.Length, "Insert remaining production types");
            context.ChangeTracker.Clear();
            Require(AppDbQueries.All<SettingEntry>(context).Length > 0, "Materialize SettingEntry");
            Require(AppDbQueries.All<User>(context).Length > 0, "Materialize User");
            Require(AppDbQueries.All<Achievement>(context).Length > 0, "Materialize Achievement");
            Require(AppDbQueries.All<AchievementArchive>(context).Length > 0, "Materialize AchievementArchive");
            Require(AppDbQueries.All<BackpackArchive>(context).Length > 0, "Materialize BackpackArchive");
            Require(AppDbQueries.All<BackpackItem>(context).Length > 0, "Materialize BackpackItem");
            Require(AppDbQueries.All<GachaItem>(context).Length > 0, "Materialize GachaItem");
            Require(AppDbQueries.All<BeyondGachaItem>(context).Length > 0, "Materialize BeyondGachaItem");
            Require(AppDbQueries.All<GachaArchive>(context).Length > 0, "Materialize GachaArchive");
            Require(AppDbQueries.All<AvatarInfo>(context).Length > 0, "Materialize AvatarInfo");
            Require(AppDbQueries.All<GameAccount>(context).Length > 0, "Materialize GameAccount");
            Require(AppDbQueries.All<DailyNoteEntry>(context).Length > 0, "Materialize DailyNoteEntry");
            Require(AppDbQueries.All<ObjectCacheEntry>(context).Length > 0, "Materialize ObjectCacheEntry");
            Require(AppDbQueries.All<CultivateProject>(context).Length > 0, "Materialize CultivateProject");
            Require(AppDbQueries.All<CultivateEntry>(context).Length > 0, "Materialize CultivateEntry");
            Require(AppDbQueries.All<CultivateEntryLevelInformation>(context).Length > 0, "Materialize CultivateEntryLevelInformation");
            Require(AppDbQueries.All<CultivateItem>(context).Length > 0, "Materialize CultivateItem");
            Require(AppDbQueries.All<InventoryItem>(context).Length > 0, "Materialize InventoryItem");
            Require(AppDbQueries.All<SpiralAbyssEntry>(context).Length > 0, "Materialize SpiralAbyssEntry");
            Require(AppDbQueries.All<UidProfilePicture>(context).Length > 0, "Materialize UidProfilePicture");
            Require(AppDbQueries.All<RoleCombatEntry>(context).Length > 0, "Materialize RoleCombatEntry");
            Require(AppDbQueries.All<AvatarStrategy>(context).Length > 0, "Materialize AvatarStrategy");
            Require(AppDbQueries.All<HardChallengeEntry>(context).Length > 0, "Materialize HardChallengeEntry");
            Require(AppDbQueries.All<BackpackReliquaryScoreConfig>(context).Length > 0, "Materialize BackpackReliquaryScoreConfig");
            Require(AppDbQueries.All<AvatarReliquaryScoreSetting>(context).Length > 0, "Materialize AvatarReliquaryScoreSetting");
            Require(!context.ChangeTracker.Entries().Any(), "Untracked queries");
            Require(AppDbQueries.GetUsers(context).Single().SToken!.ToString() == user.SToken.ToString(), "Cookie converter");
            Require(AppDbQueries.GetBackpackItems(context).Single().EquippedAvatarId is null, "Nullable scalar");
            Require(AppDbQueries.GetDailyNotes(context).Single().ResinNotifyThreshold == 100, "Observable partial property");
            RoundTrip(avatar, AppDbQueries.GetAvatarInfos(context).Single().Info2!);
            RoundTrip(dailyNote, AppDbQueries.GetDailyNotes(context).Single().DailyNote!);
            RoundTrip(spiralAbyss, AppDbQueries.GetSpiralAbysses(context).Single().SpiralAbyss);
            RoundTrip(roleCombat, AppDbQueries.GetRoleCombats(context).Single().RoleCombatData);
            RoundTrip(hardChallenge, AppDbQueries.GetHardChallenges(context).Single().HardChallengeData);
            VerifyBusinessQueries(context, user, achievementArchive, backpackArchive, gachaArchive, project, entry);
            AppDbQueries.WriteSetting(context, "aot-setting", "updated");
            Require(AppDbQueries.ReadSetting(context, "aot-setting")?.Value == "updated", "Parameterized UPSERT update");
            AppDbQueries.WriteSetting(context, "different'key", null);
            Require(AppDbQueries.ReadSetting(context, "different'key") is { Value: null } && AppDbQueries.ReadSetting(context, "missing") is null, "Changed parameters and nullable value");
            User updated = AppDbQueries.GetUsers(context).Single();
            updated.Aid = "updated-aid";
            context.Update(updated);
            Require(context.SaveChanges() == 1, "Disconnected update");
            context.ChangeTracker.Clear();
            Require(AppDbQueries.GetUsers(context).Single().Aid == "updated-aid", "Update persisted");
            context.Remove(AppDbQueries.GetObjectCache(context).Single());
            Require(context.SaveChanges() == 1 && AppDbQueries.GetObjectCache(context).Length == 0, "Disconnected delete");
            Console.WriteLine("PASS: 25 real entity types, CRUD, five JSON converters, cookies, parameterized settings.");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }

    private static T Fixture<T>(string name)
    {
        using Stream stream = typeof(DatabaseVerification).Assembly.GetManifestResourceStream("Hutao.Database.Fixtures." + name + ".json") ?? throw new InvalidOperationException("Missing fixture " + name);
        using StreamReader reader = new(stream);
        string json = reader.ReadToEnd();
        T value = PersistenceJson.Deserialize<T>(json) ?? throw new InvalidOperationException("Null fixture " + name);
        string extended = json.Insert(json.LastIndexOf('}'), ",\n\"future_field\": { \"nested\": [1, 2, 3] }\n");
        Require(PersistenceJson.Serialize(value) == PersistenceJson.Serialize(PersistenceJson.Deserialize<T>(extended)), "Unknown JSON fields remain compatible: " + name);
        return value;
    }

    private static void RoundTrip<T>(T expected, T actual) => Require(PersistenceJson.Serialize(expected) == PersistenceJson.Serialize(actual), "JSON round-trip " + typeof(T).Name);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
