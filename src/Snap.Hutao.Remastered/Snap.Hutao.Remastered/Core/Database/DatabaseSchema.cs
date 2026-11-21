// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Data.Sqlite;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Snap.Hutao.Remastered.Core.Database;

internal static class DatabaseSchema
{
    public const string UserDatabaseFileName = "Userdata.db";

    public const string PendingUserDatabaseFileName = "Userdata.pending-import.db";

    public static int Upgrade(string connectionString, string? targetMigration = null)
    {
        return Upgrade(connectionString, targetMigration, requireHistory: false);
    }

    public static bool HasUserDatabase(string directory)
    {
        string path = Path.Combine(directory, UserDatabaseFileName);
        return File.Exists(path) || HasJournalFiles(path);
    }

    public static void StageUserDatabaseImport(string sourceDirectory, string destinationDirectory)
    {
        string stagingPath = Path.Combine(destinationDirectory, $"Userdata.import-{Guid.NewGuid():N}.db");
        try
        {
            CopyUserDatabase(Path.Combine(sourceDirectory, UserDatabaseFileName), stagingPath);
            Upgrade(new SqliteConnectionStringBuilder { DataSource = stagingPath }.ToString(), targetMigration: null, requireHistory: true);
            File.Move(stagingPath, Path.Combine(destinationDirectory, PendingUserDatabaseFileName), overwrite: true);
        }
        finally
        {
            File.Delete(stagingPath);
        }
    }

    public static void ApplyPendingUserDatabaseImport(string databasePath)
    {
        string directory = Path.GetDirectoryName(databasePath)!;
        string pendingPath = Path.Combine(directory, PendingUserDatabaseFileName);
        if (!File.Exists(pendingPath))
        {
            return;
        }

        string pendingConnectionString = new SqliteConnectionStringBuilder { DataSource = pendingPath, Mode = SqliteOpenMode.ReadWrite }.ToString();
        Upgrade(pendingConnectionString, targetMigration: null, requireHistory: true);

        if (HasJournalFiles(databasePath) && File.Exists(databasePath))
        {
            // Fold the old WAL into the old database before storing it as a standalone backup.
            SqliteConnectionStringBuilder builder = new() { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, Pooling = false };
            using SqliteConnection existing = new(builder.ToString());
            existing.Open();
            SetDeleteJournalMode(existing);
        }

        if (HasJournalFiles(databasePath))
        {
            throw new IOException("The current user database still has journal files. Close other clients using that folder and try again.");
        }

        if (File.Exists(databasePath))
        {
            string backupPath = Path.Combine(directory, $"Userdata.before-import-{Guid.NewGuid():N}.db");
            File.Replace(pendingPath, databasePath, backupPath);
        }
        else
        {
            File.Move(pendingPath, databasePath);
        }
    }

    private static bool HasJournalFiles(string databasePath)
    {
        return File.Exists(databasePath + "-wal") || File.Exists(databasePath + "-shm") || File.Exists(databasePath + "-journal");
    }

    public static void CopyUserDatabase(string sourcePath, string destinationPath)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        };
        using SqliteConnection source = new(builder.ToString());
        source.Open();

        builder.DataSource = destinationPath;
        builder.Mode = SqliteOpenMode.ReadWriteCreate;
        using SqliteConnection destination = new(builder.ToString());
        destination.Open();
        source.BackupDatabase(destination);

        // Publish a standalone file even if the source uses write-ahead logging.
        SetDeleteJournalMode(destination);
    }

    private static void SetDeleteJournalMode(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = DELETE;";
        if (command.ExecuteScalar() is not "delete")
        {
            throw new InvalidDataException("Could not close the database journal.");
        }
    }

    private static int Upgrade(string connectionString, string? targetMigration, bool requireHistory)
    {
        int targetCount = DatabaseMigrations.All.Length;
        if (targetMigration is not null)
        {
            int index = Array.FindIndex(DatabaseMigrations.All, migration => migration.Id == targetMigration);
            if (index < 0)
            {
                throw new ArgumentException("The target database migration is unknown.", nameof(targetMigration));
            }
            targetCount = index + 1;
        }

        // SQLite cannot change foreign_keys inside a transaction. Table rebuilds need
        // it disabled on this private connection; all constraints are checked before commit.
        SqliteConnectionStringBuilder builder = new(connectionString) { ForeignKeys = false, Pooling = false };
        using SqliteConnection connection = new(builder.ToString());
        connection.Open();

        // The write lock precedes the history read, so concurrent app starts cannot both
        // decide that the same migration is pending. Disposal rolls back every failure.
        using SqliteTransaction transaction = connection.BeginTransaction(deferred: false);
        List<string> applied = ReadHistory(connection, transaction);
        if (requireHistory && applied.Count == 0)
        {
            throw new InvalidDataException("The selected database has no Snap Hutao migration history.");
        }

        if (applied.Count > targetCount)
        {
            throw new InvalidDataException($"This build cannot accept the database migration history: the database has {applied.Count} records ending at '{applied[^1]}'; the requested target recognizes {targetCount} records ending at '{DatabaseMigrations.All[targetCount - 1].Id}'. Unknown histories and schema downgrades are not supported. The database was not changed.");
        }
        for (int index = 0; index < applied.Count; index++)
        {
            if (applied[index] != DatabaseMigrations.All[index].Id)
            {
                throw new InvalidDataException($"The database migration history is unknown or incomplete: record {index + 1} is '{applied[index]}', expected '{DatabaseMigrations.All[index].Id}'. The database was not changed.");
            }
        }

        if (applied.Count == targetCount)
        {
            transaction.Commit();
            return 0;
        }

        Execute(connection, transaction, """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """);

        for (int index = applied.Count; index < targetCount; index++)
        {
            DatabaseMigration migration = DatabaseMigrations.All[index];
            string sql = ReadScript(migration);
            if (!string.IsNullOrWhiteSpace(sql)) Execute(connection, transaction, sql);
            using SqliteCommand history = connection.CreateCommand();
            history.Transaction = transaction;
            history.CommandText = "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ($id, $version);";
            history.Parameters.AddWithValue("$id", migration.Id);
            history.Parameters.AddWithValue("$version", DatabaseMigrations.EfProductVersion);
            history.ExecuteNonQuery();
        }

        using (SqliteCommand check = connection.CreateCommand())
        {
            check.Transaction = transaction;
            check.CommandText = "PRAGMA foreign_key_check;";
            using SqliteDataReader reader = check.ExecuteReader();
            if (reader.Read())
            {
                throw new InvalidDataException($"Database migration would leave an invalid foreign key in table '{reader.GetString(0)}'. All changes were rolled back.");
            }
        }

        transaction.Commit();
        return targetCount - applied.Count;
    }

    private static List<string> ReadHistory(SqliteConnection connection, SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name = '__EFMigrationsHistory';";
        if ((long)command.ExecuteScalar()! == 0)
        {
            command.CommandText = "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND name != '__EFMigrationsLock';";
            if ((long)command.ExecuteScalar()! != 0)
            {
                throw new InvalidDataException("A nonempty database has no migration history. It was not changed.");
            }
            return [];
        }

        command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId COLLATE BINARY;";
        using SqliteDataReader reader = command.ExecuteReader();
        List<string> result = [];
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }
        return result;
    }

    private static string ReadScript(DatabaseMigration migration)
    {
        using Stream stream = typeof(DatabaseSchema).Assembly.GetManifestResourceStream(migration.ResourceName)
            ?? throw new InvalidDataException($"Database migration resource '{migration.Id}' is missing.");
        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string sql = reader.ReadToEnd().Replace("\r\n", "\n");
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
        if (hash != migration.Sha256)
        {
            throw new InvalidDataException($"Database migration resource '{migration.Id}' failed its integrity check.");
        }
        return sql;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

internal sealed record DatabaseMigration(string Id, string ResourceName, string Sha256);
