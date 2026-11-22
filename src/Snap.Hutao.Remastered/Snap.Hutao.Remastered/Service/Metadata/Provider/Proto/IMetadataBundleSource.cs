// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Ensures a local bundle file exists and returns its path. A separate contract so a future git or
/// snapshot source can replace HTTP without touching <see cref="ProtoMetadataProvider"/>.
/// </summary>
public interface IMetadataBundleSource
{
    /// <summary>
    /// Returns the local bundle path, or null when no usable bundle is available (no network and no
    /// cached file). Never deletes the previous bundle on failure.
    /// </summary>
    ValueTask<string?> EnsureLocalBundleAsync(CancellationToken token = default);
}
