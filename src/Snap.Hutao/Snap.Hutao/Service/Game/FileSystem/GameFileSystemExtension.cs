// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Core.IO.Ini;
using System.IO;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Service.Game.FileSystem;

internal static class GameFileSystemExtension
{
    private static readonly ConditionalWeakTable<IGameFileSystemView, string> GameFileSystemGameFileNames = [];
    private static readonly ConditionalWeakTable<IGameFileSystemView, string> GameFileSystemGameDirectories = [];
    private static readonly ConditionalWeakTable<IGameFileSystemView, string> GameFileSystemGameConfigurationFilePaths = [];
    private static readonly ConditionalWeakTable<IGameFileSystemView, string> GameFileSystemPcGameSdkFilePaths = [];
    private static readonly ConditionalWeakTable<IGameFileSystemView, string> GameFileSystemScreenShotDirectories = [];
    private static readonly ConditionalWeakTable<IGameFileSystemView, string> GameFileSystemDataDirectories = [];
    private static readonly ConditionalWeakTable<IGameFileSystemView, string> GameFileSystemScriptVersionFilePaths = [];
    private static readonly ConditionalWeakTable<IGameFileSystemView, string> GameFileSystemChunksDirectories = [];
    private static readonly ConditionalWeakTable<IGameFileSystemView, string> GameFileSystemPredownloadStatusPaths = [];

    extension(IGameFileSystemView gameFileSystem)
    {
        public string GameFileName
        {
            get
            {
                ObjectDisposedException.ThrowIf(gameFileSystem.IsDisposed, gameFileSystem);

                if (GameFileSystemGameFileNames.TryGetValue(gameFileSystem, out string? gameFileName))
                {
                    return gameFileName;
                }

                gameFileName = string.Intern(Path.GetFileName(gameFileSystem.GameFilePath));
                GameFileSystemGameFileNames.Add(gameFileSystem, gameFileName);
                return gameFileName;
            }
        }

        public string GameDirectory
        {
            get
            {
                ObjectDisposedException.ThrowIf(gameFileSystem.IsDisposed, gameFileSystem);

                if (GameFileSystemGameDirectories.TryGetValue(gameFileSystem, out string? gameDirectory))
                {
                    return gameDirectory;
                }

                gameDirectory = Path.GetDirectoryName(gameFileSystem.GameFilePath);
                ArgumentException.ThrowIfNullOrEmpty(gameDirectory);
                string internedGameDirectory = string.Intern(gameDirectory);
                GameFileSystemGameDirectories.Add(gameFileSystem, internedGameDirectory);
                return internedGameDirectory;
            }
        }

        public string GameConfigurationFilePath
        {
            get
            {
                ObjectDisposedException.ThrowIf(gameFileSystem.IsDisposed, gameFileSystem);

                if (GameFileSystemGameConfigurationFilePaths.TryGetValue(gameFileSystem, out string? gameConfigFilePath))
                {
                    return gameConfigFilePath;
                }

                gameConfigFilePath = string.Intern(Path.Combine(gameFileSystem.GameDirectory, GameConstants.ConfigFileName));
                GameFileSystemGameConfigurationFilePaths.Add(gameFileSystem, gameConfigFilePath);
                return gameConfigFilePath;
            }
        }

        public string PCGameSDKFilePath
        {
            get
            {
                ObjectDisposedException.ThrowIf(gameFileSystem.IsDisposed, gameFileSystem);

                if (GameFileSystemPcGameSdkFilePaths.TryGetValue(gameFileSystem, out string? pcGameSdkFilePath))
                {
                    return pcGameSdkFilePath;
                }

                pcGameSdkFilePath = string.Intern(Path.Combine(gameFileSystem.GameDirectory, GameConstants.PCGameSDKFilePath));
                GameFileSystemPcGameSdkFilePaths.Add(gameFileSystem, pcGameSdkFilePath);
                return pcGameSdkFilePath;
            }
        }

        public string ScreenShotDirectory
        {
            get
            {
                ObjectDisposedException.ThrowIf(gameFileSystem.IsDisposed, gameFileSystem);

                if (GameFileSystemScreenShotDirectories.TryGetValue(gameFileSystem, out string? screenShotDirectory))
                {
                    return screenShotDirectory;
                }

                screenShotDirectory = string.Intern(Path.Combine(gameFileSystem.GameDirectory, "ScreenShot"));
                GameFileSystemScreenShotDirectories.Add(gameFileSystem, screenShotDirectory);
                return screenShotDirectory;
            }
        }

        public string DataDirectory
        {
            get
            {
                ObjectDisposedException.ThrowIf(gameFileSystem.IsDisposed, gameFileSystem);

                if (GameFileSystemDataDirectories.TryGetValue(gameFileSystem, out string? dataDirectory))
                {
                    return dataDirectory;
                }

                string dataDirectoryName = gameFileSystem.IsExecutableOversea ? GameConstants.GenshinImpactData : GameConstants.YuanShenData;
                dataDirectory = string.Intern(Path.Combine(gameFileSystem.GameDirectory, dataDirectoryName));
                GameFileSystemDataDirectories.Add(gameFileSystem, dataDirectory);
                return dataDirectory;
            }
        }

        public string ScriptVersionFilePath
        {
            get
            {
                ObjectDisposedException.ThrowIf(gameFileSystem.IsDisposed, gameFileSystem);

                if (GameFileSystemScriptVersionFilePaths.TryGetValue(gameFileSystem, out string? scriptVersionFilePath))
                {
                    return scriptVersionFilePath;
                }

                scriptVersionFilePath = string.Intern(Path.Combine(gameFileSystem.DataDirectory, "Persistent", "ScriptVersion"));
                GameFileSystemScriptVersionFilePaths.Add(gameFileSystem, scriptVersionFilePath);
                return scriptVersionFilePath;
            }
        }

        public string ChunksDirectory
        {
            get
            {
                ObjectDisposedException.ThrowIf(gameFileSystem.IsDisposed, gameFileSystem);

                if (GameFileSystemChunksDirectories.TryGetValue(gameFileSystem, out string? chunksDirectory))
                {
                    return chunksDirectory;
                }

                chunksDirectory = string.Intern(Path.Combine(gameFileSystem.GameDirectory, "chunks"));
                GameFileSystemChunksDirectories.Add(gameFileSystem, chunksDirectory);
                return chunksDirectory;
            }
        }

        public string PredownloadStatusFilePath
        {
            get
            {
                ObjectDisposedException.ThrowIf(gameFileSystem.IsDisposed, gameFileSystem);

                if (GameFileSystemPredownloadStatusPaths.TryGetValue(gameFileSystem, out string? predownloadStatusPath))
                {
                    return predownloadStatusPath;
                }

                predownloadStatusPath = string.Intern(Path.Combine(gameFileSystem.ChunksDirectory, "snap_hutao_predownload_status.json"));
                GameFileSystemPredownloadStatusPaths.Add(gameFileSystem, predownloadStatusPath);
                return predownloadStatusPath;
            }
        }

        public bool IsExecutableOversea
        {
            get
            {
                ObjectDisposedException.ThrowIf(gameFileSystem.IsDisposed, gameFileSystem);

                string gameFileName = gameFileSystem.GameFileName;
                return gameFileName.ToUpperInvariant() switch
                {
                    GameConstants.GenshinImpactFileNameUpper => true,
                    GameConstants.YuanShenFileNameUpper => false,
                    _ => throw HutaoException.Throw($"Invalid game executable file name：{gameFileName}"),
                };
            }
        }

        public bool TryGetGameVersion([NotNullWhen(true)] out string? version)
        {
            version = default!;
            string configFilePath = gameFileSystem.GameConfigurationFilePath;
            if (File.Exists(configFilePath))
            {
                foreach (ref readonly IniElement element in IniSerializer.DeserializeFromFile(configFilePath).AsSpan())
                {
                    if (element is IniParameter { Key: GameConstants.GameVersion, Value: { Length: > 0 } value })
                    {
                        version = value;
                        return true;
                    }
                }
            }

            string scriptVersionFilePath = gameFileSystem.ScriptVersionFilePath;
            if (File.Exists(scriptVersionFilePath))
            {
                version = File.ReadAllText(scriptVersionFilePath);
                return true;
            }

            return false;
        }
    }
}