// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Git;

/// <summary>
/// The git/JSON source. The extra member exists for diagnostics only; consumers never depend on it.
/// </summary>
public interface IGitMetadataProvider : IMetadataProvider
{
    /// <summary>
    /// The cloned Snap.Metadata working tree, or an empty string before initialization.
    /// </summary>
    string RepositoryDirectory { get; }
}
