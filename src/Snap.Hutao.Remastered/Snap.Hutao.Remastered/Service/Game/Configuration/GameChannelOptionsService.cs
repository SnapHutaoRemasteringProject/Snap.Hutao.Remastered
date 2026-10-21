// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Game.FileSystem;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Game.Configuration;

[Service(ServiceLifetime.Singleton, typeof(IGameChannelOptionsService))]
public sealed partial class GameChannelOptionsService : IGameChannelOptionsService
{
    private readonly LaunchOptions launchOptions;

    [GeneratedConstructor]
    public partial GameChannelOptionsService(IServiceProvider serviceProvider);

    public ChannelOptions GetChannelOptions()
    {
        const string LockTrace = $"{nameof(GameChannelOptionsService)}.{nameof(GetChannelOptions)}";
        GameFileSystemErrorKind errorKind = launchOptions.TryGetGameFileSystem(LockTrace, out IGameFileSystem? gameFileSystem);
        switch (errorKind)
        {
            case GameFileSystemErrorKind.GamePathNullOrEmpty:
                return ChannelOptions.GamePathNullOrEmpty();
            case GameFileSystemErrorKind.GamePathLocked:
                return ChannelOptions.GamePathLocked(launchOptions.GamePathEntry.Value?.Path ?? string.Empty);
        }

        ArgumentNullException.ThrowIfNull(gameFileSystem);
        using (gameFileSystem)
        {
            // Channel detection reads only config.ini; version metadata and module
            // compatibility are independent concerns, and inspection never repairs files.
            try { return GameConfiguration.Read(gameFileSystem); }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                return ChannelOptions.ConfigurationFileNotFound(gameFileSystem.GameConfigurationFilePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                System.Diagnostics.Trace.TraceWarning("Could not read game channel configuration: {0}", ex);
                return ChannelOptions.ConfigurationFileReadFailed(gameFileSystem.GameConfigurationFilePath);
            }
        }
    }
}