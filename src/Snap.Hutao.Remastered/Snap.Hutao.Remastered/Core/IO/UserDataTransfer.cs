// Copyright (c) Snap Hutao Remastering Project. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Database;
using System.IO;
using System.IO.Compression;

namespace Snap.Hutao.Remastered.Core.IO;

internal enum UserDataMigrationStage
{
    Compress,
    Transfer,
    Extract,
}

internal static class UserDataTransfer
{
    private const string StagingDirectoryPrefix = ".hutao-transfer-";
    private const string MigrationBackupPrefix = "Hutao.MigrationBackup-";

    private static readonly string[] UnpackagedStateEntries =
    [
        "settings.json", "settings.json.tmp", "diagnostics_settings.json", "Cache", "CrashLogs", "Diagnostics", "LocalFolder",
    ];

    private static readonly EnumerationOptions DirectoryEntries = new()
    {
        AttributesToSkip = 0,
        IgnoreInaccessible = false,
    };

    public static void CopyToDataFolder(string sourcePath, string destinationPath, bool replaceExisting, IProgress<(UserDataMigrationStage Stage, double? Percentage)> progress)
    {
        progress.Report((UserDataMigrationStage.Compress, null));
        FileSystemInfo[] entries = GetDataEntries(sourcePath);
        if (RuntimeEnvironment.IsUnpackaged
            && (IsUnpackagedStateDirectory(sourcePath) || IsUnpackagedStateDirectory(destinationPath)))
        {
            entries = entries.Where(entry => !IsUnpackagedStateEntry(sourcePath, entry.FullName)).ToArray();
        }

        string sourceStaging = CreateStagingDirectory(sourcePath);
        try
        {
            if (Directory.Exists(Path.Combine(destinationPath, Path.GetFileName(sourceStaging))))
            {
                throw new IOException(SH.ViewModelSettingStorageSetDataFolderDescription2);
            }

            string sourceArchive = Path.Combine(sourceStaging, "Userdata.zip");
            CreateArchive(sourcePath, entries, sourceArchive, value => progress.Report((UserDataMigrationStage.Compress, value)));
            progress.Report((UserDataMigrationStage.Compress, 100));
            string destinationStaging = CreateStagingDirectory(destinationPath);
            try
            {
                string destinationArchive = Path.Combine(destinationStaging, "Userdata.zip");
                progress.Report((UserDataMigrationStage.Transfer, null));
                MoveArchive(sourceArchive, destinationArchive, value => progress.Report((UserDataMigrationStage.Transfer, value)));

                string extractedPath = Directory.CreateDirectory(Path.Combine(destinationStaging, "Data")).FullName;
                ExtractArchive(destinationArchive, extractedPath, value => progress.Report((UserDataMigrationStage.Extract, value)));
                File.Delete(destinationArchive);
                PublishDataFolder(extractedPath, destinationPath, replaceExisting);
                progress.Report((UserDataMigrationStage.Extract, 100));
            }
            finally
            {
                Directory.Delete(destinationStaging, recursive: true);
            }
        }
        finally
        {
            Directory.Delete(sourceStaging, recursive: true);
        }
    }

    public static void SaveBackup(string sourcePath, string archivePath)
    {
        FileSystemInfo[] entries = GetDataEntries(sourcePath);
        string stagingDirectory = CreateStagingDirectory(Path.GetDirectoryName(archivePath)!);
        string stagingArchive = Path.Combine(stagingDirectory, "backup.zip");
        try
        {
            CreateArchive(sourcePath, entries, stagingArchive);
            File.Move(stagingArchive, archivePath);
        }
        finally
        {
            Directory.Delete(stagingDirectory, recursive: true);
        }
    }

    private static string CreateStagingDirectory(string path)
    {
        return Directory.CreateDirectory(Path.Combine(path, $"{StagingDirectoryPrefix}{Guid.NewGuid():N}")).FullName;
    }

    private static void CreateArchive(string sourcePath, FileSystemInfo[] entries, string archivePath, Action<double>? updateProgress = null)
    {
        using ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        int total = entries.Length + 1;
        updateProgress?.Invoke(0);
        for (int index = 0; index < entries.Length; index++)
        {
            FileSystemInfo entry = entries[index];
            string name = Path.GetRelativePath(sourcePath, entry.FullName).Replace('\\', '/');
            if (entry is DirectoryInfo)
            {
                archive.CreateEntry(name + "/");
            }
            else
            {
                using FileStream source = File.Open(entry.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                ZipArchiveEntry archiveEntry = archive.CreateEntry(name, CompressionLevel.Fastest);
                DateTimeOffset lastWrite = entry.LastWriteTime;
                if (lastWrite.Year is >= 1980 and <= 2107)
                {
                    archiveEntry.LastWriteTime = lastWrite;
                }

                using Stream target = archiveEntry.Open();
                source.CopyTo(target);
            }

            ReportEntryProgress(updateProgress, index + 1, total);
        }

        if (DatabaseSchema.HasUserDatabase(sourcePath))
        {
            string snapshot = Path.Combine(Path.GetDirectoryName(archivePath)!, DatabaseSchema.UserDatabaseFileName);
            try
            {
                DatabaseSchema.CopyUserDatabase(Path.Combine(sourcePath, DatabaseSchema.UserDatabaseFileName), snapshot);
                archive.CreateEntryFromFile(snapshot, DatabaseSchema.UserDatabaseFileName, CompressionLevel.Fastest);
            }
            finally
            {
                File.Delete(snapshot);
            }
        }
    }

    private static void ExtractArchive(string archivePath, string destinationPath, Action<double> updateProgress)
    {
        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        int total = archive.Entries.Count + 1;
        string destinationPrefix = destinationPath + Path.DirectorySeparatorChar;
        updateProgress(0);
        for (int index = 0; index < archive.Entries.Count; index++)
        {
            ZipArchiveEntry entry = archive.Entries[index];
            string path = Path.GetFullPath(entry.FullName, destinationPath);
            if (!path.StartsWith(destinationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Invalid path in migration archive: {entry.FullName}");
            }

            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                entry.ExtractToFile(path);
            }

            ReportEntryProgress(updateProgress, index + 1, total);
        }
    }

    private static void MoveArchive(string sourcePath, string destinationPath, Action<double> updateProgress)
    {
        if (string.Equals(Path.GetPathRoot(sourcePath), Path.GetPathRoot(destinationPath), StringComparison.OrdinalIgnoreCase))
        {
            File.Move(sourcePath, destinationPath);
            return;
        }

        using (FileStream source = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, options: FileOptions.SequentialScan))
        using (FileStream destination = new(destinationPath, FileMode.CreateNew, FileAccess.Write))
        {
            byte[] buffer = new byte[1024 * 1024];
            long length = source.Length;
            int lastPercentage = 0;
            updateProgress(0);
            int bytesRead;
            while ((bytesRead = source.Read(buffer)) > 0)
            {
                destination.Write(buffer.AsSpan(0, bytesRead));
                int percentage = (int)(100d * source.Position / length);
                if (percentage > lastPercentage)
                {
                    updateProgress(percentage);
                    lastPercentage = percentage;
                }
            }
        }

        File.Delete(sourcePath);
    }

    private static void ReportEntryProgress(Action<double>? updateProgress, int completed, int total)
    {
        if (completed % Math.Max(1, total / 100) == 0)
        {
            updateProgress?.Invoke(100d * completed / total);
        }
    }

    private static FileSystemInfo[] GetDataEntries(string sourcePath)
    {
        string databasePath = Path.GetFullPath(Path.Combine(sourcePath, DatabaseSchema.UserDatabaseFileName));
        return EnumerateDataEntries(new DirectoryInfo(sourcePath))
            .Where(entry => !IsDatabaseFile(entry.FullName, databasePath))
            .ToArray();
    }

    private static bool IsUnpackagedStateDirectory(string path)
    {
        return string.Equals(Path.TrimEndingDirectorySeparator(path), AppDataPaths.UnpackagedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnpackagedStateEntry(string root, string path)
    {
        string relativePath = Path.GetRelativePath(root, path);
        int separator = relativePath.IndexOf(Path.DirectorySeparatorChar);
        string topLevelName = separator < 0 ? relativePath : relativePath[..separator];
        return UnpackagedStateEntries.Contains(topLevelName, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<FileSystemInfo> EnumerateDataEntries(DirectoryInfo root)
    {
        Stack<DirectoryInfo> directories = new();
        directories.Push(root);
        while (directories.TryPop(out DirectoryInfo? directory))
        {
            foreach (FileSystemInfo entry in directory.EnumerateFileSystemInfos("*", DirectoryEntries))
            {
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0 && entry.LinkTarget is not null)
                {
                    continue;
                }

                if (entry is DirectoryInfo child)
                {
                    // Keep temporary files and rollback copies out of new archives.
                    if (HasTransferDirectoryName(child.Name, StagingDirectoryPrefix)
                        || HasTransferDirectoryName(child.Name, MigrationBackupPrefix))
                    {
                        continue;
                    }

                    directories.Push(child);
                }

                yield return entry;
            }
        }
    }

    private static bool HasTransferDirectoryName(string name, string prefix)
    {
        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && Guid.TryParseExact(name.AsSpan(prefix.Length), "N", out _);
    }

    private static bool IsDatabaseFile(string path, string databasePath)
    {
        return string.Equals(path, databasePath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, databasePath + "-wal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, databasePath + "-shm", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, databasePath + "-journal", StringComparison.OrdinalIgnoreCase);
    }

    private static void PublishDataFolder(string stagingPath, string destinationPath, bool replaceExisting)
    {
        FileSystemInfo[] entries = new DirectoryInfo(stagingPath).GetFileSystemInfos();
        List<string> targetNames = entries.Select(entry => entry.Name).ToList();
        if (File.Exists(Path.Combine(stagingPath, DatabaseSchema.UserDatabaseFileName)))
        {
            targetNames.Add(DatabaseSchema.UserDatabaseFileName + "-wal");
            targetNames.Add(DatabaseSchema.UserDatabaseFileName + "-shm");
            targetNames.Add(DatabaseSchema.UserDatabaseFileName + "-journal");
            targetNames.Add(DatabaseSchema.PendingUserDatabaseFileName);
        }
        else if (DatabaseSchema.HasUserDatabase(destinationPath))
        {
            throw new IOException(SH.ViewModelSettingStorageSetDataFolderHasDatabase);
        }

        string[] conflicts = targetNames.Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(name => Path.Exists(Path.Combine(destinationPath, name)))
            .ToArray();
        if (conflicts.Length > 0 && !replaceExisting)
        {
            throw new IOException(SH.FormatViewModelSettingStorageMigrationConflict(Path.Combine(destinationPath, conflicts[0])));
        }

        string? backupPath = conflicts.Length == 0 ? null
            : Directory.CreateDirectory(Path.Combine(destinationPath, $"{MigrationBackupPrefix}{Guid.NewGuid():N}")).FullName;
        List<(string Source, string Destination)> moves = [];
        try
        {
            foreach (string name in conflicts)
            {
                string source = Path.Combine(destinationPath, name);
                string destination = Path.Combine(backupPath!, name);
                MoveEntry(source, destination);
                moves.Add((source, destination));
            }

            foreach (FileSystemInfo entry in entries)
            {
                string destination = Path.Combine(destinationPath, entry.Name);
                MoveEntry(entry.FullName, destination);
                moves.Add((entry.FullName, destination));
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            List<Exception> errors = [error];
            for (int index = moves.Count - 1; index >= 0; index--)
            {
                (string source, string destination) = moves[index];
                try
                {
                    MoveEntry(destination, source);
                }
                catch (Exception rollbackError) when (rollbackError is IOException or UnauthorizedAccessException)
                {
                    errors.Add(rollbackError);
                }
            }

            if (errors.Count > 1)
            {
                throw new AggregateException(errors);
            }

            throw;
        }
    }

    private static void MoveEntry(string sourcePath, string destinationPath)
    {
        if (Directory.Exists(sourcePath))
        {
            Directory.Move(sourcePath, destinationPath);
        }
        else
        {
            File.Move(sourcePath, destinationPath);
        }
    }
}
