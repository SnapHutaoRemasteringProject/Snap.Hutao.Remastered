using Microsoft.Data.Sqlite;
using Snap.Hutao.Remastered.Core.Database;
using System.Runtime.CompilerServices;

if (RuntimeFeature.IsDynamicCodeSupported || RuntimeFeature.IsDynamicCodeCompiled)
{
    throw new InvalidOperationException("Publish and run the database verification project with Native AOT.");
}

string directory = Path.Combine(Path.GetTempPath(), "HutaoDatabaseAot", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    string Fresh(string name) => new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, name + ".db"), Pooling = false }.ToString();

    string fresh = Fresh("fresh");
    Require(DatabaseSchema.Upgrade(fresh) == DatabaseMigrations.All.Length, "Fresh migration count");
    Require(DatabaseSchema.Upgrade(fresh) == 0, "Idempotent startup");
    Require(Scalar(fresh, "SELECT COUNT(*) FROM sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%';") == 25, "All 25 application tables");

    foreach (DatabaseMigration migration in DatabaseMigrations.All)
    {
        string legacy = Fresh(migration.Id);
        DatabaseSchema.Upgrade(legacy, migration.Id);
        Execute(legacy, "INSERT INTO settings (\"Key\", \"Value\") VALUES ('AotRegression', '胡桃旧数据');");
        DatabaseSchema.Upgrade(legacy);
        Require(Text(legacy, "SELECT Value FROM settings WHERE Key='AotRegression';") == "胡桃旧数据", "Legacy setting retention: " + migration.Id);
        Require(Scalar(legacy, "SELECT COUNT(*) FROM __EFMigrationsHistory;") == DatabaseMigrations.All.Length, "Legacy history: " + migration.Id);
    }

    string rollback = Fresh("rollback");
    DatabaseSchema.Upgrade(rollback, DatabaseMigrations.All[0].Id);
    Execute(rollback, "CREATE TABLE achievement_archives (Sentinel TEXT); INSERT INTO settings VALUES ('keep', 'original');");
    Expect<SqliteException>(() => DatabaseSchema.Upgrade(rollback));
    Require(Scalar(rollback, "SELECT COUNT(*) FROM __EFMigrationsHistory;") == 1, "Failed SQL history rollback");
    Require(Scalar(rollback, "SELECT COUNT(*) FROM sqlite_schema WHERE name='achievements';") == 0, "Failed SQL DDL rollback");
    Require(Text(rollback, "SELECT Value FROM settings WHERE Key='keep';") == "original", "Failed SQL data rollback");

    string foreignKey = Fresh("foreign-key");
    DatabaseSchema.Upgrade(foreignKey, DatabaseMigrations.All[0].Id);
    Execute(foreignKey, "PRAGMA foreign_keys=OFF; CREATE TABLE orphan (Parent TEXT REFERENCES settings(Key)); INSERT INTO orphan VALUES ('missing');");
    Expect<InvalidDataException>(() => DatabaseSchema.Upgrade(foreignKey));
    Require(Scalar(foreignKey, "SELECT COUNT(*) FROM __EFMigrationsHistory;") == 1, "Foreign key failure rolls back the upgrade");

    string future = Fresh("future");
    DatabaseSchema.Upgrade(future);
    Execute(future, "INSERT INTO __EFMigrationsHistory VALUES ('99999999999999_Future', 'future');");
    Expect<InvalidDataException>(() => DatabaseSchema.Upgrade(future));
    Require(Scalar(future, "SELECT COUNT(*) FROM __EFMigrationsHistory;") == DatabaseMigrations.All.Length + 1, "Newer database untouched");

    string gap = Fresh("gap");
    DatabaseSchema.Upgrade(gap);
    Execute(gap, "DELETE FROM __EFMigrationsHistory WHERE MigrationId=(SELECT MIN(MigrationId) FROM __EFMigrationsHistory);");
    Expect<InvalidDataException>(() => DatabaseSchema.Upgrade(gap));

    string unversioned = Fresh("unversioned");
    Execute(unversioned, "CREATE TABLE user_data (Value TEXT); INSERT INTO user_data VALUES ('keep');");
    Expect<InvalidDataException>(() => DatabaseSchema.Upgrade(unversioned));
    Require(Text(unversioned, "SELECT Value FROM user_data;") == "keep", "Unversioned database untouched");

    string concurrent = Fresh("concurrent");
    Task<int>[] starts = [Task.Run(() => DatabaseSchema.Upgrade(concurrent)), Task.Run(() => DatabaseSchema.Upgrade(concurrent))];
    int[] applied = await Task.WhenAll(starts);
    Require(applied.Sum() == DatabaseMigrations.All.Length, "Concurrent starts apply each migration once");
    Require(Scalar(concurrent, "SELECT COUNT(*) FROM __EFMigrationsHistory;") == DatabaseMigrations.All.Length, "Concurrent history");

    Console.WriteLine($"PASS: Native AOT SQLite schema; {DatabaseMigrations.All.Length} historical versions upgraded; 25 tables; idempotence, SQL/DDL rollback, foreign-key validation, future/gapped/unversioned history rejection and concurrent starts.");
}
finally
{
    Directory.Delete(directory, recursive: true);
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void Expect<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }
    throw new InvalidOperationException("Expected " + typeof(T).Name);
}

static void Execute(string connectionString, string sql)
{
    using SqliteConnection connection = new(connectionString);
    connection.Open();
    using SqliteCommand command = connection.CreateCommand();
    command.CommandText = sql;
    command.ExecuteNonQuery();
}

static long Scalar(string connectionString, string sql)
{
    using SqliteConnection connection = new(connectionString);
    connection.Open();
    using SqliteCommand command = connection.CreateCommand();
    command.CommandText = sql;
    return (long)command.ExecuteScalar()!;
}

static string? Text(string connectionString, string sql)
{
    using SqliteConnection connection = new(connectionString);
    connection.Open();
    using SqliteCommand command = connection.CreateCommand();
    command.CommandText = sql;
    return command.ExecuteScalar() as string;
}
