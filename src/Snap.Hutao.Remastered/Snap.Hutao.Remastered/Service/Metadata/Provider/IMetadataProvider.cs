// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider;

/// <summary>
/// One metadata source. A provider only prepares its own data and reads categories out of it; it
/// does not know who calls it, which other sources exist, or whether a failure causes a fallback.
/// </summary>
public interface IMetadataProvider
{
    /// <summary>
    /// The source this provider serves. Never <see cref="MetadataSourceKind.Auto"/>.
    /// </summary>
    MetadataSourceKind Kind { get; }

    bool IsInitialized { get; }

    MetadataCache Cache { get; }

    /// <summary>
    /// Prepares the source (clone/fetch a repository, ensure/decode a bundle). Returns false when the
    /// source is unavailable; only cancellation is thrown. A failure is not cached, so the manager
    /// can retry later.
    /// </summary>
    ValueTask<bool> InitializeAsync(CancellationToken token = default);

    ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token = default)
        where T : class;

    /// <summary>
    /// Clears this provider's derived cache and releases any parsed in-memory representation.
    /// </summary>
    void ClearCache();
}
