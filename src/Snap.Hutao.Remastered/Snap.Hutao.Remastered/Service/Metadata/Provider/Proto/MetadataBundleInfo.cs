// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// What the initialized proto provider knows about its bundle, for settings/diagnostics.
/// </summary>
public sealed class MetadataBundleInfo
{
    public required string Publisher { get; init; }

    public required int VersionMajor { get; init; }

    public required int VersionMinor { get; init; }

    public required int VersionPatch { get; init; }

    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    /// <c>0</c> means the producer did not version the schema yet.
    /// </summary>
    public required int SchemaVersion { get; init; }

    public required string Language { get; init; }

    public required string Hash { get; init; }

    public required long Size { get; init; }

    /// <summary>
    /// File the data came from (the downloaded bundle or the developer override path).
    /// </summary>
    public required string Path { get; init; }

    public string Version
    {
        get => VersionPatch > 0 ? $"{VersionMajor}.{VersionMinor}.{VersionPatch}" : $"{VersionMajor}.{VersionMinor}";
    }
}
