using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Snap.Hutao.Remastered.Core.Database;
using Snap.Hutao.Remastered.Model.Entity.Database;
using System.Globalization;

namespace Snap.Hutao.Remastered.Service;

internal static class SchemaVerifier
{
    public static void Verify()
    {
        string directory = Path.Combine(Path.GetTempPath(), "HutaoSchemaParity", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string reference = ConnectionString(Path.Combine(directory, "reference.db"));
            using (AppDbContext context = CreateContext(reference))
            {
                context.GetService<IMigrator>().Migrate();
            }
            string expected = DescribeSchema(reference);
            foreach (DatabaseMigration migration in DatabaseMigrations.All)
            {
                string legacy = ConnectionString(Path.Combine(directory, migration.Id + ".db"));
                using (AppDbContext context = CreateContext(legacy))
                {
                    // Create the old database through EF itself, not through the new runner.
                    context.GetService<IMigrator>().Migrate(migration.Id);
                }
                using (SqliteConnection connection = new(legacy))
                {
                    connection.Open();
                    using SqliteCommand command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO settings (Key, Value) VALUES ('AotRegression', '胡桃旧数据');";
                    command.ExecuteNonQuery();
                }
                string? existingSeedId = null;
                if (string.CompareOrdinal(migration.Id, "20260805125557_AddBackpackReliquaryScoreConfig") >= 0)
                {
                    existingSeedId = ReadSeedId(legacy);
                }
                DatabaseSchema.Upgrade(legacy);
                if (DescribeSchema(legacy) != expected)
                {
                    throw new InvalidOperationException("Schema differs from EF migration output: " + migration.Id);
                }
                if (existingSeedId is not null && ReadSeedId(legacy) != existingSeedId)
                {
                    throw new InvalidOperationException("Existing score configuration identity changed: " + migration.Id);
                }
                using (SqliteConnection connection = new(legacy))
                {
                    connection.Open();
                    using SqliteCommand command = connection.CreateCommand();
                    command.CommandText = "SELECT Value FROM settings WHERE Key='AotRegression';";
                    if (command.ExecuteScalar() as string != "胡桃旧数据")
                    {
                        throw new InvalidOperationException("Legacy data changed: " + migration.Id);
                    }
                }
            }
            Console.WriteLine($"PASS: {DatabaseMigrations.All.Length} EF-created historical databases upgraded with identical tables, columns, indexes and foreign keys; legacy settings retained.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static AppDbContext CreateContext(string connectionString)
    {
        // This tool intentionally has no application model. Historical target snapshots
        // are the migration authority; comparing them to an empty host model is meaningless.
        return new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)).Options);
    }

    private static string ConnectionString(string path) => new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();

    private static string? ReadSeedId(string connectionString)
    {
        using SqliteConnection connection = new(connectionString);
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT InnerId FROM backpack_reliquary_score_config WHERE PresetKey=0 LIMIT 1;";
        return command.ExecuteScalar() as string;
    }

    private static string DescribeSchema(string connectionString)
    {
        using SqliteConnection connection = new(connectionString);
        connection.Open();
        string[] tables = Rows(connection, "SELECT name FROM sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != '__EFMigrationsLock' ORDER BY name;").Select(row => row[0]).ToArray();
        List<string> descriptions = [];
        foreach (string table in tables)
        {
            string quoted = "\"" + table.Replace("\"", "\"\"") + "\"";
            descriptions.Add("TABLE " + table);
            descriptions.AddRange(Rows(connection, "PRAGMA table_xinfo(" + quoted + ");").Select(row => string.Join('|', row)));
            descriptions.AddRange(Rows(connection, "PRAGMA foreign_key_list(" + quoted + ");").Select(row => "FK " + string.Join('|', row)).Order(StringComparer.Ordinal));
            string[][] indexes = Rows(connection, "PRAGMA index_list(" + quoted + ");").OrderBy(row => row[1], StringComparer.Ordinal).ToArray();
            foreach (string[] index in indexes)
            {
                descriptions.Add("INDEX " + string.Join('|', index.Skip(1)));
                descriptions.AddRange(Rows(connection, "PRAGMA index_xinfo(\"" + index[1].Replace("\"", "\"\"") + "\");").Select(row => string.Join('|', row)));
            }
        }
        return string.Join('\n', descriptions);
    }

    private static List<string[]> Rows(SqliteConnection connection, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        using SqliteDataReader reader = command.ExecuteReader();
        List<string[]> result = [];
        while (reader.Read())
        {
            string[] row = new string[reader.FieldCount];
            for (int index = 0; index < row.Length; index++)
            {
                row[index] = Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture) ?? "NULL";
            }
            result.Add(row);
        }
        return result;
    }
}
