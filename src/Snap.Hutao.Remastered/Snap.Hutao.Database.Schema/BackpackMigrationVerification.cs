using Microsoft.Data.Sqlite;
using Snap.Hutao.Remastered.Core.Database;
using System.Globalization;

namespace Snap.Hutao.Remastered.DatabaseTooling;

internal static class BackpackMigrationVerification
{
    private const string Previous = "20260925133234_AddAvatarReliquaryScoreSetting";
    private const string Repair = "20261022144006_MakeBackpackEquippedAvatarIdNullable";

    public static void Run(string directory)
    {
        foreach (string variant in new[] { "released-not-null", "edited-nullable", "already-applied" })
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(directory, variant + ".db"),
                Pooling = false,
                ForeignKeys = true,
            }.ToString();
            DatabaseSchema.Upgrade(connectionString, variant == "already-applied" ? Repair : Previous);
            if (variant == "released-not-null") RestoreReleasedColumn(connectionString);

            Execute(connectionString, """
                INSERT INTO backpack_archives VALUES ('00000000-0000-0000-0000-000000000001', 'legacy', 1);
                INSERT INTO backpack_items (InnerId, ArchiveId, ItemId, Guid, Count, Level, PromoteLevel, RefinementRank, IsLocked, IsMarked, EquippedAvatarId, AppendPropIdListJson)
                VALUES ('00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 100, 123, 5, 20, 2, 1, 1, 0, 0, '[1,2]'),
                       ('00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 101, 456, 6, 40, 3, 2, 0, 1, 10000001, '[3,4]');
                UPDATE __EFMigrationsHistory SET ProductVersion='10.0.6' WHERE MigrationId='20220720121642_Init';
                UPDATE __EFMigrationsHistory SET ProductVersion='11.0.0-rc.2.26504.105' WHERE MigrationId='20261022144006_MakeBackpackEquippedAvatarIdNullable';
                """);
            if (variant != "released-not-null")
            {
                Execute(connectionString, "UPDATE backpack_items SET EquippedAvatarId=NULL WHERE ItemId=100;");
            }

            string items = Items(connectionString);
            string archives = Rows(connectionString, "SELECT * FROM backpack_archives ORDER BY InnerId;");
            string history = Rows(connectionString, "SELECT MigrationId,ProductVersion FROM __EFMigrationsHistory WHERE MigrationId!='" + Repair + "' ORDER BY MigrationId;");
            string indexes = Rows(connectionString, "SELECT name,sql FROM sqlite_schema WHERE type='index' AND tbl_name='backpack_items' ORDER BY name;");
            string foreignKeys = Rows(connectionString, "PRAGMA foreign_key_list(backpack_items);");
            if (variant == "released-not-null")
            {
                Execute(connectionString, "CREATE TRIGGER reject_repair BEFORE INSERT ON __EFMigrationsHistory WHEN NEW.MigrationId='" + Repair + "' BEGIN SELECT RAISE(ABORT, 'reject repair history'); END;");
                bool rejected = false;
                try { DatabaseSchema.Upgrade(connectionString); }
                catch (SqliteException) { rejected = true; }
                Require(rejected, "Injected history write must fail");
                Require(Items(connectionString) == items, "History write failure retains item data");
                Require(Rows(connectionString, "SELECT \"notnull\" FROM pragma_table_info('backpack_items') WHERE name='EquippedAvatarId';") == "1", "History write failure rolls back the table rebuild");
                Require(Rows(connectionString, "SELECT MigrationId,ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;") == history, "History write failure retains complete old history");
                Execute(connectionString, "DROP TRIGGER reject_repair;");
            }
            Require(DatabaseSchema.Upgrade(connectionString) == (variant == "already-applied" ? 0 : 1), variant + " pending count");
            Require(Items(connectionString) == items, variant + " complete item data retained");
            Require(Rows(connectionString, "SELECT * FROM backpack_archives ORDER BY InnerId;") == archives, variant + " archive retained");
            Require(Rows(connectionString, "SELECT MigrationId,ProductVersion FROM __EFMigrationsHistory WHERE MigrationId!='" + Repair + "' ORDER BY MigrationId;") == history, variant + " old history and EF versions retained");
            Require(Rows(connectionString, "SELECT name,sql FROM sqlite_schema WHERE type='index' AND tbl_name='backpack_items' ORDER BY name;") == indexes, variant + " indexes retained");
            Require(Rows(connectionString, "PRAGMA foreign_key_list(backpack_items);") == foreignKeys, variant + " foreign keys retained");
            Require(Rows(connectionString, "PRAGMA foreign_key_check;") == "", variant + " foreign key check");
            Require(Rows(connectionString, "SELECT \"notnull\" FROM pragma_table_info('backpack_items') WHERE name='EquippedAvatarId';") == "0", variant + " ownership nullable");
            if (variant == "already-applied")
            {
                Require(Rows(connectionString, "SELECT ProductVersion FROM __EFMigrationsHistory WHERE MigrationId='" + Repair + "';") == "11.0.0-rc.2.26504.105", "Existing repair version must not be rewritten");
            }

            Execute(connectionString, "UPDATE backpack_items SET EquippedAvatarId=NULL WHERE ItemId=101;");
            using SqliteConnection connection = new(connectionString);
            connection.Open();
            using SqliteTransaction transaction = connection.BeginTransaction();
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM backpack_archives; SELECT COUNT(*) FROM backpack_items;";
            Require(Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 0, variant + " cascade works");
            transaction.Rollback();
            Require(Rows(connectionString, "SELECT COUNT(*) FROM backpack_items;") == "2", variant + " cascade rollback restores items");
            Require(DatabaseSchema.Upgrade(connectionString) == 0, variant + " second startup no-op");
        }
        Console.WriteLine("PASS: released NOT NULL/edited nullable 47-record databases and already-applied 48-record rc.2 history; item data, null/zero/owned states, archives, indexes, foreign keys, old EF versions and cascade rollback retained.");
    }

    private static void RestoreReleasedColumn(string connectionString)
    {
        string create = Rows(connectionString, "SELECT sql FROM sqlite_schema WHERE type='table' AND name='backpack_items';");
        const string nullableColumn = "\"EquippedAvatarId\" INTEGER NULL";
        Require(create.Contains(nullableColumn, StringComparison.Ordinal), "Expected current production nullable column");
        string legacy = create.Replace("CREATE TABLE \"backpack_items\"", "CREATE TABLE \"released_backpack_items\"", StringComparison.Ordinal)
            .Replace(nullableColumn, "\"EquippedAvatarId\" INTEGER NOT NULL DEFAULT 0", StringComparison.Ordinal);
        // Reproduce the actual pre-7dee0520b released constraint, not a substitute entity/model.
        Execute(connectionString, legacy + "; INSERT INTO released_backpack_items SELECT * FROM backpack_items; DROP TABLE backpack_items; ALTER TABLE released_backpack_items RENAME TO backpack_items; CREATE INDEX \"IX_backpack_items_ArchiveId\" ON \"backpack_items\" (\"ArchiveId\");");
        Require(Rows(connectionString, "SELECT \"notnull\" FROM pragma_table_info('backpack_items') WHERE name='EquippedAvatarId';") == "1", "Released NOT NULL fixture");
    }

    private static string Items(string connectionString) => Rows(connectionString, "SELECT InnerId,ArchiveId,ItemId,Guid,Count,Level,PromoteLevel,RefinementRank,EquippedAvatarId,MainPropId,AppendPropIdListJson,PurchasedAppendPropIdListJson,DefiniteAppendPropIdListJson,IsLocked,IsMarked FROM backpack_items ORDER BY InnerId;");

    private static void Execute(string connectionString, string sql)
    {
        using SqliteConnection connection = new(connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string Rows(string connectionString, string sql)
    {
        using SqliteConnection connection = new(connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        using SqliteDataReader reader = command.ExecuteReader();
        List<string> rows = [];
        while (reader.Read())
        {
            rows.Add(string.Join('|', Enumerable.Range(0, reader.FieldCount).Select(index => Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture))));
        }
        return string.Join('\n', rows);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
