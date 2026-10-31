// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.StaticResource;

/// <summary>
/// The outcome of one static resource archive download.
/// </summary>
public enum StaticResourceDownloadStatus
{
    /// <summary>The archive was downloaded, extracted and stored.</summary>
    Success,

    /// <summary>The caller's token fired before the archive was completed.</summary>
    Canceled,

    /// <summary>The transfer delivered no data for longer than the stall timeout.</summary>
    Stalled,

    /// <summary>
    /// Every attempt answered with a media type that cannot be an archive. The static resource
    /// server answers some throttling or error pages with a 2xx status, so this replaces retrying
    /// a body that is not the requested file.
    /// </summary>
    UnsupportedContent,

    /// <summary>The transfer or the extraction failed; see <see cref="StaticResourceDownloadResult.Exception"/>.</summary>
    Failed,
}
