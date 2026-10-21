// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO.Ini;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Game.Configuration;

// Pure inspection: no restore, patch, or injected-module lookup is performed.
public static class GameInstallationReader
{
    public static GameInstallationInfo Read(string gamePath, string configPath, string scriptPath, bool isOversea)
    {
        string? channel = null;
        string? subChannel = null;
        string? configText = null;
        string? configError = null;
        GameVersionFileStatus configStatus;
        Version? configVersion = null;
        try
        {
            foreach (IniElement element in IniSerializer.DeserializeFromFile(configPath))
            {
                if (element is not IniParameter parameter) { continue; }
                if (string.Equals(parameter.Key, "game_version", StringComparison.OrdinalIgnoreCase)) { configText ??= parameter.Value; }
                if (string.Equals(parameter.Key, ChannelOptions.ChannelName, StringComparison.OrdinalIgnoreCase)) { channel = parameter.Value; }
                if (string.Equals(parameter.Key, ChannelOptions.SubChannelName, StringComparison.OrdinalIgnoreCase)) { subChannel = parameter.Value; }
            }
            configStatus = Version.TryParse(configText?.Trim(), out configVersion) ? GameVersionFileStatus.Valid : GameVersionFileStatus.Invalid;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            configStatus = GameVersionFileStatus.Missing;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            configStatus = GameVersionFileStatus.Unreadable;
            configError = ex.Message;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException)
        {
            configStatus = GameVersionFileStatus.Invalid;
            configError = ex.Message;
        }

        Version? scriptVersion = null;
        string? scriptError = null;
        GameVersionFileStatus scriptStatus;
        try
        {
            scriptStatus = Version.TryParse(File.ReadAllText(scriptPath).Trim(), out scriptVersion) ? GameVersionFileStatus.Valid : GameVersionFileStatus.Invalid;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            scriptStatus = GameVersionFileStatus.Missing;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            scriptStatus = GameVersionFileStatus.Unreadable;
            scriptError = ex.Message;
        }

        ChannelOptions channels = configStatus switch
        {
            GameVersionFileStatus.Missing => ChannelOptions.ConfigurationFileNotFound(configPath),
            GameVersionFileStatus.Unreadable => ChannelOptions.ConfigurationFileReadFailed(configPath),
            _ => new(channel, subChannel, isOversea),
        };
        return new()
        {
            GamePath = gamePath,
            IsOversea = isOversea,
            Channel = channels,
            ConfigVersion = configVersion,
            ScriptVersion = scriptVersion,
            ConfigStatus = configStatus,
            ScriptStatus = scriptStatus,
            ConfigReadError = configError,
            ScriptReadError = scriptError,
        };
    }
}
