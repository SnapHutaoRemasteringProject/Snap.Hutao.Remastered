// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider;

/// <summary>
/// Creates or returns the keyed provider singleton for a source. This is the only place that
/// resolves a provider by key, so <see cref="Service.Metadata.MetadataService"/> never depends on a
/// concrete provider type.
/// </summary>
public interface IMetadataProviderFactory
{
    ImmutableArray<MetadataSourceKind> AvailableKinds { get; }

    IMetadataProvider GetOrCreate(MetadataSourceKind kind);
}
