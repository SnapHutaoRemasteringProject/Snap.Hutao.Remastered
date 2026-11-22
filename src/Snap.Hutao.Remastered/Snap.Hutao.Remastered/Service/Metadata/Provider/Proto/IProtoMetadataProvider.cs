// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// The proto bundle source. The extra members exist for diagnostics; consumers never depend on them.
/// </summary>
public interface IProtoMetadataProvider : IMetadataProvider
{
    /// <summary>
    /// Header information of the decoded bundle, or null before a successful initialization.
    /// </summary>
    MetadataBundleInfo? BundleInfo { get; }

    /// <summary>
    /// The local bundle file currently in use, or an empty string before initialization.
    /// </summary>
    string BundleFilePath { get; }
}
