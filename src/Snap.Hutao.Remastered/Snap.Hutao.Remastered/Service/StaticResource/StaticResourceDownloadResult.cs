// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.StaticResource;

/// <summary>
/// The result of <see cref="StaticResourceDownloader.DownloadAndExtractAsync"/>.
/// </summary>
/// <param name="Status">What happened during the download.</param>
/// <param name="Exception">The failure, when <see cref="Status"/> is <see cref="StaticResourceDownloadStatus.Failed"/>.</param>
/// <param name="Message">A user presentable failure detail, when the transport could produce one.</param>
public readonly record struct StaticResourceDownloadResult(
    StaticResourceDownloadStatus Status,
    Exception? Exception,
    string? Message)
{
    public static StaticResourceDownloadResult Success { get; } = new(StaticResourceDownloadStatus.Success, null, null);

    public static StaticResourceDownloadResult Canceled { get; } = new(StaticResourceDownloadStatus.Canceled, null, null);

    public static StaticResourceDownloadResult Stalled { get; } = new(StaticResourceDownloadStatus.Stalled, null, null);

    public static StaticResourceDownloadResult UnsupportedContent { get; } = new(StaticResourceDownloadStatus.UnsupportedContent, null, null);

    public static StaticResourceDownloadResult Failed(Exception exception, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new(StaticResourceDownloadStatus.Failed, exception, message);
    }
}
