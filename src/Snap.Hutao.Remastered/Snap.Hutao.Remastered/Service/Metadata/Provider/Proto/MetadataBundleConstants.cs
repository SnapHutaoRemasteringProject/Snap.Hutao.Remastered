// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Format and transport constants of the proto bundle. Kept separate from
/// <see cref="MetadataBundleOptions"/> so the codec and the manifest can be compiled and tested without
/// the application's runtime dependencies.
/// </summary>
internal static class MetadataBundleConstants
{
    public const string BundleFileName = "GenshinMetadata.hutaobin";

    public const string TempFileSuffix = ".tmp";

    public const string ETagFileSuffix = ".etag";

    /// <summary>
    /// A single-locale bundle is a few MiB; the combined bundle is under 64 MiB. Anything larger is
    /// treated as a corrupt or malicious payload.
    /// </summary>
    public const int MaxEncryptedBytes = 64 * 1024 * 1024;

    public static readonly TimeSpan MaxCacheAge = TimeSpan.FromDays(7);

    public static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(2);

    public static readonly TimeSpan CachedBundleRefreshTimeout = TimeSpan.FromSeconds(30);
}
