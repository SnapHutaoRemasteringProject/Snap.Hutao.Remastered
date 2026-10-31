// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Caching;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Collections.Frozen;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Mime;
using System.Text;

namespace Snap.Hutao.Remastered.Service.StaticResource;

/// <summary>
/// Downloads one static resource archive and stores its entries in the image cache.
/// </summary>
/// <remarks>
/// The type owns the transport policy: the retry budget, the accepted media types and the stall
/// timeout. It deliberately has no UI dependencies, so the caller decides how to present a
/// <see cref="StaticResourceDownloadResult"/>.
/// </remarks>
[Service(ServiceLifetime.Transient)]
public sealed class StaticResourceDownloader
{
    private const int MaxAttempts = 3;

    private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(10);

    private static readonly FrozenSet<string?> AllowedMediaTypes =
    [
        MediaTypeNames.Application.Octet,
        MediaTypeNames.Application.Zip,

        // Super hacking, we now upload zip files as images
        MediaTypeNames.Image.Jpeg,
    ];

    private readonly HttpClient httpClient;
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly IImageCache imageCache;

    public StaticResourceDownloader(
        HttpClient httpClient,
        IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory,
        IImageCache imageCache)
    {
        this.httpClient = httpClient;
        this.httpRequestMessageBuilderFactory = httpRequestMessageBuilderFactory;
        this.imageCache = imageCache;
    }

    /// <summary>
    /// Downloads <paramref name="fileName"/> and stores every archive entry in the image cache.
    /// </summary>
    /// <param name="fileName">The archive name the static resource endpoint serves.</param>
    /// <param name="progress">Receives the copy progress; the caller owns its thread affinity.</param>
    /// <param name="token">Cancels the transfer.</param>
    /// <returns>
    /// The outcome of the transfer. The method reports failures through the result instead of
    /// throwing them, so the caller never has to translate transport exceptions.
    /// </returns>
    public async ValueTask<StaticResourceDownloadResult> DownloadAndExtractAsync(
        string fileName,
        IProgress<StreamCopyStatus> progress,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(progress);

        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory
            .Create()
            .SetRequestUri(StaticResourcesEndpoints.StaticZip(fileName))
            .SetStaticResourceControlHeaders()
            .Get();

        try
        {
            await builder.PrepareForRetryAsync(token).ConfigureAwait(false);

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                token.ThrowIfCancellationRequested();

                builder.RecreateRequest();

                TimeSpan retryDelay = default;
                using (HttpRequestMessage request = builder.HttpRequestMessage)
                using (HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    if (!AllowedMediaTypes.Contains(response.Content.Headers.ContentType?.MediaType))
                    {
                        if (attempt == MaxAttempts)
                        {
                            return StaticResourceDownloadResult.UnsupportedContent;
                        }

                        retryDelay = response.Headers.RetryAfter?.Delta ?? default;
                    }
                    else
                    {
                        await DownloadAndExtractCoreAsync(response, fileName, progress, token).ConfigureAwait(false);
                        return StaticResourceDownloadResult.Success;
                    }
                }

                await Task.Delay(retryDelay, token).ConfigureAwait(false);
            }

            return StaticResourceDownloadResult.UnsupportedContent;
        }
        catch (StallDetectedException)
        {
            return StaticResourceDownloadResult.Stalled;
        }
        catch (OperationCanceledException)
        {
            return StaticResourceDownloadResult.Canceled;
        }
        catch (Exception exception)
        {
            return CreateFailure(builder, exception);
        }
    }

    private async ValueTask DownloadAndExtractCoreAsync(
        HttpResponseMessage response,
        string fileName,
        IProgress<StreamCopyStatus> progress,
        CancellationToken token)
    {
        long contentLength = response.Content.Headers.ContentLength ?? 0;

        using Stream content = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using TempFileStream tempStream = new(FileMode.OpenOrCreate, FileAccess.ReadWrite);

        using (StreamCopyWorker worker = new(new StallGuardStream(content, StallTimeout), tempStream, contentLength))
        {
            await worker.CopyAsync(progress, token).ConfigureAwait(false);
        }

        token.ThrowIfCancellationRequested();

        await ExtractFilesAsync(fileName, tempStream, token).ConfigureAwait(false);
    }

    private async ValueTask ExtractFilesAsync(string fileName, Stream stream, CancellationToken token)
    {
        using ZipArchive archive = await ZipArchive.CreateAsync(stream, ZipArchiveMode.Read, false, null, token).ConfigureAwait(false);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            using Stream source = entry.Open();
            await imageCache.StoreFileAsync(fileName, entry.FullName, source, token).ConfigureAwait(false);
        }
    }

    private static StaticResourceDownloadResult CreateFailure(HttpRequestMessageBuilder builder, Exception exception)
    {
        if (exception is IOException or UnauthorizedAccessException)
        {
            return StaticResourceDownloadResult.Failed(exception);
        }

        if (HttpRequestExceptionHandling.TryHandle(builder, exception, out StringBuilder message))
        {
            return StaticResourceDownloadResult.Failed(exception, message.ToString());
        }

        return StaticResourceDownloadResult.Failed(exception);
    }
}
