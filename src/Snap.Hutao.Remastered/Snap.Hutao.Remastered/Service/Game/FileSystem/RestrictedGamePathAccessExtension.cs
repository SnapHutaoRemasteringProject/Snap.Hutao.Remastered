// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Service.Game.PathAbstraction;
using System.Diagnostics;

namespace Snap.Hutao.Remastered.Service.Game.FileSystem;

public static class RestrictedGamePathAccessExtension
{
    extension(IRestrictedGamePathAccess access)
    {
        public GameFileSystemErrorKind TryGetGameFileSystem(string trace, out IGameFileSystem? fileSystem)
        {
            string? gamePath = access.GamePathEntry.Value?.Path;

            if (string.IsNullOrEmpty(gamePath))
            {
                fileSystem = default;
                SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateDebug($"[{trace}] Error: GamePathNullOrEmpty", "TryGetGameFileSystem"));
                return GameFileSystemErrorKind.GamePathNullOrEmpty;
            }

            if (!access.GamePathLock.TryReaderLock(trace, out AsyncReaderWriterLock.Releaser releaser))
            {
                fileSystem = default;
                SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateDebug($"[{trace}] Error: GamePathLocked", "TryGetGameFileSystem"));
                Debug.WriteLine($"Game path is locked, {access.GamePathLock}");
                return GameFileSystemErrorKind.GamePathLocked;
            }

            fileSystem = GameFileSystem.Create(gamePath, releaser);
            SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateDebug($"[{trace}] Succeed", "TryGetGameFileSystem"));
            return GameFileSystemErrorKind.None;
        }

        /// <summary>
        /// 同步游戏路径条目。如果路径已存在于条目列表中，则选中已有条目；
        /// 否则将新路径添加到列表中并选中。
        /// </summary>
        /// <param name="gamePath">游戏路径或 Shell URI。如果为 <c>null</c>，则使用当前选中的路径。</param>
        /// <param name="launchMethod">新路径对应的启动方式，仅在添加新条目时使用。</param>
        /// <returns>同步后的最终游戏路径。</returns>
        public string PerformGamePathEntrySynchronization(string? gamePath = default, GameLaunchMethod launchMethod = GameLaunchMethod.Executable)
        {
            gamePath ??= access.GamePathEntry.Value?.Path;

            // The game path is null or empty, this means no game path is selected
            if (string.IsNullOrEmpty(gamePath))
            {
                access.GamePathEntry.Value = default;
                return string.Empty;
            }

            // The game path is in the entries
            if (access.GamePathEntries.Value.SingleOrDefault(entry => string.Equals(entry.Path, gamePath, StringComparison.OrdinalIgnoreCase)) is { } existed)
            {
                // Prefer the existed entry value
                access.GamePathEntry.Value = existed;
                return existed.Path;
            }

            // We need update the entries when game path not in the entries.
            const string LockTrace = $"{nameof(RestrictedGamePathAccessExtension)}.{nameof(PerformGamePathEntrySynchronization)}";
            if (!access.GamePathLock.TryWriterLock(LockTrace, out AsyncReaderWriterLock.Releaser releaser))
            {
                throw HutaoException.InvalidOperation($"Cannot set game path entries while it is being used. {access.GamePathLock}");
            }

            using (access.GamePathEntry.GetDeferral())
            {
                using (access.GamePathEntries.GetDeferral())
                {
                    using (releaser)
                    {
                        // The game path is not in the entries, add it to the entries.
                        GamePathEntry newEntry = GamePathEntry.Create(gamePath, launchMethod);
                        access.GamePathEntries.Value = access.GamePathEntries.Value.Add(newEntry);
                        access.GamePathEntry.Value = newEntry;
                    }
                }
            }

            return gamePath;
        }

        public string RemoveGamePathEntry(GamePathEntry? entry)
        {
            if (entry is not null)
            {
                const string LockTrace = $"{nameof(RestrictedGamePathAccessExtension)}.{nameof(RemoveGamePathEntry)}";
                if (!access.GamePathLock.TryWriterLock(LockTrace, out AsyncReaderWriterLock.Releaser releaser))
                {
                    throw HutaoException.InvalidOperation($"Cannot remove game path while it is being used. {access.GamePathLock}");
                }

                using (releaser)
                {
                    // Clear game path if it's selected.
                    if (string.Equals(access.GamePathEntry.Value?.Path, entry.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        access.GamePathEntry.Value = default;
                    }

                    access.GamePathEntries.Value = access.GamePathEntries.Value.Remove(entry);
                }
            }

            return access.PerformGamePathEntrySynchronization();
        }

        public IGameFileSystem UnsafeForceUpdateGamePath(string gamePath, IGameFileSystem old)
        {
            old.Dispose();
            access.PerformGamePathEntrySynchronization(gamePath);
            access.TryGetGameFileSystem($"{nameof(RestrictedGamePathAccessExtension)}.{nameof(UnsafeForceUpdateGamePath)}", out IGameFileSystem? newFileSystem);
            ArgumentNullException.ThrowIfNull(newFileSystem);
            return newFileSystem;
        }
    }


}
