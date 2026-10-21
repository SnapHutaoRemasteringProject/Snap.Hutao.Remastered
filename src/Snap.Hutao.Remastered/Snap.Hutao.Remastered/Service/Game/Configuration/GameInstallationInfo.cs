// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Game.Configuration;

public sealed record GameInstallationInfo
{
    public required string GamePath { get; init; }
    public required bool IsOversea { get; init; }
    public required ChannelOptions Channel { get; init; }
    public Version? ConfigVersion { get; init; }
    public Version? ScriptVersion { get; init; }
    public GameVersionFileStatus ConfigStatus { get; init; }
    public GameVersionFileStatus ScriptStatus { get; init; }
    public string? ConfigReadError { get; init; }
    public string? ScriptReadError { get; init; }

    public bool HasVersionConflict { get => ConfigVersion is not null && ScriptVersion is not null && Normalize(ConfigVersion) != Normalize(ScriptVersion); }
    public bool HasVersionProblem { get => HasVersionConflict || ConfigStatus != GameVersionFileStatus.Valid || ScriptStatus != GameVersionFileStatus.Valid; }

    // A missing source can use the remaining valid source. A conflicting,
    // unreadable or malformed source must never be silently treated as older.
    public Version? InstalledVersion
    {
        get => !HasVersionConflict && ConfigStatus is GameVersionFileStatus.Valid or GameVersionFileStatus.Missing
            && ScriptStatus is GameVersionFileStatus.Valid or GameVersionFileStatus.Missing ? ConfigVersion ?? ScriptVersion : null;
    }

    public bool MatchesVersionFiles(GameInstallationInfo other)
    {
        return string.Equals(GamePath, other.GamePath, StringComparison.OrdinalIgnoreCase)
            && ConfigStatus == other.ConfigStatus && ScriptStatus == other.ScriptStatus
            && Equals(ConfigVersion, other.ConfigVersion) && Equals(ScriptVersion, other.ScriptVersion)
            && Channel.Channel == other.Channel.Channel && Channel.SubChannel == other.Channel.SubChannel && IsOversea == other.IsOversea;
    }

    private static Version Normalize(Version version)
    {
        return new(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));
    }
}
