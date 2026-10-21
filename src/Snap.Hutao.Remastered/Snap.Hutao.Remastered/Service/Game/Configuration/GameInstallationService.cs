// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Snap.Hutao.Remastered.Service.Game.FileSystem;

namespace Snap.Hutao.Remastered.Service.Game.Configuration;

[Service(ServiceLifetime.Singleton)]
public sealed class GameInstallationService(LaunchOptions launchOptions, ITaskContext taskContext, ILogger<GameInstallationService> logger)
{
    public GameInstallationInfo Read(IGameFileSystemView fileSystem)
    {
        GameInstallationInfo info = GameInstallationReader.Read(fileSystem.GameFilePath, fileSystem.GameConfigurationFilePath, fileSystem.ScriptVersionFilePath, fileSystem.IsExecutableOversea);
        if (info.ConfigReadError is not null) { logger.LogWarning("Could not read game configuration: {Error}", info.ConfigReadError); }
        if (info.ScriptReadError is not null) { logger.LogWarning("Could not read game script version: {Error}", info.ScriptReadError); }
        return info;
    }

    public async ValueTask<GameInstallationInfo?> RefreshAsync(CancellationToken token = default)
    {
        await taskContext.SwitchToBackgroundAsync();
        token.ThrowIfCancellationRequested();
        const string LockTrace = $"{nameof(GameInstallationService)}.{nameof(RefreshAsync)}";
        if (launchOptions.TryGetGameFileSystem(LockTrace, out IGameFileSystem? fileSystem) != GameFileSystemErrorKind.None) { return null; }
        using (fileSystem)
        {
            token.ThrowIfCancellationRequested();
            return Read(fileSystem!);
        }
    }
}
