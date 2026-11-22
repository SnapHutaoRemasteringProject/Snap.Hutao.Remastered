// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata;

/// <summary>
/// The consumer-facing metadata service. It owns the provider instances, picks a source by
/// preference, falls back and broadcasts cache clears; consumers never see a provider.
/// </summary>
public interface IMetadataService : IMetadataServiceInitialization
{
    bool IsInitialized { get; }

    /// <summary>
    /// The active provider kind, or <see cref="MetadataSourceKind.Auto"/> while nothing is active.
    /// </summary>
    MetadataSourceKind ActiveSource { get; }

    /// <summary>
    /// The sources this build can use, for settings/diagnostics.
    /// </summary>
    IReadOnlyList<MetadataSourceKind> AvailableSources { get; }

    /// <summary>
    /// The active provider's cache. Throws when metadata is not initialized yet.
    /// </summary>
    MetadataCache Cache { get; }

    /// <summary>
    /// Ensures metadata is initialized. Self-starting and idempotent: if no initialization has been
    /// started, this call starts one.
    /// </summary>
    ValueTask<bool> InitializeAsync();

    /// <summary>
    /// Activates <paramref name="kind"/> using the same candidate and fallback rules as startup.
    /// <see cref="MetadataSourceKind.Auto"/> restores automatic selection. This does not persist the
    /// preference; the owner of <see cref="MetadataSourceOptions"/> does.
    /// </summary>
    ValueTask<bool> SwitchAsync(MetadataSourceKind kind, CancellationToken token = default);

    ValueTask<ImmutableArray<T>> FromCacheOrSourceAsync<T>(MetadataCategory category, CancellationToken token = default)
        where T : class;

    /// <summary>
    /// Clears every created provider's derived cache. Does not reset initialization state, delete
    /// files or change the source.
    /// </summary>
    void ClearCache();
}
