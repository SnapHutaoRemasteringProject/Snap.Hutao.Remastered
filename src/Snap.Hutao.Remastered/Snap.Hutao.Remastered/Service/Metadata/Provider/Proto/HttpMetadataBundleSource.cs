// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Service.BackgroundActivity;
using Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Wire;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using BackgroundActivityTask = Snap.Hutao.Remastered.Service.BackgroundActivity.BackgroundActivity;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Downloads and validates <c>GenshinMetadata.{locale}.hutaobin</c> from
/// <c>{api}/static/metadata</c>.
/// </summary>
/// <remarks>
/// There is no manifest on the producer side yet, so a local bundle is revalidated by the server's
/// ETag and a download is validated by size bounds and by decoding it before it replaces the previous
/// file. A transient network problem or a bad publish therefore never removes a still-valid cached
/// bundle.
/// </remarks>
[Service(ServiceLifetime.Singleton, typeof(IMetadataBundleSource))]
[HttpClient(HttpClientConfiguration.Default)]
public sealed partial class HttpMetadataBundleSource : IMetadataBundleSource
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly IHutaoEndpointsFactory hutaoEndpointsFactory;
    private readonly MetadataBundleOptions bundleOptions;
    private readonly CultureOptions cultureOptions;
    private readonly BackgroundActivityOptions backgroundActivityOptions;
    private readonly ITaskContext taskContext;
    private readonly ILogger<HttpMetadataBundleSource> logger;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial HttpMetadataBundleSource(IServiceProvider serviceProvider, HttpClient httpClient);

    public async ValueTask<string?> EnsureLocalBundleAsync(CancellationToken token = default)
    {
        using SentrySpanScope span = SentrySpanScope.StartOperation("metadata.bundle.ensure", "metadata");

        if (bundleOptions.OverrideBundlePath is { } overridePath)
        {
            span.SetData("metadata.bundle.override", true);
            if (File.Exists(overridePath))
            {
                return overridePath;
            }

            logger.LogWarning("Metadata bundle override path does not exist");
            return null;
        }

        string bundlePath = bundleOptions.BundlePath;
        RemoveExpiredCache(bundlePath);
        bool hasFreshCachedBundle = IsCacheFresh(bundlePath);
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token, XamlApplicationLifetime.ShutdownToken);
        timeout.CancelAfter(
            hasFreshCachedBundle
                ? MetadataBundleConstants.CachedBundleRefreshTimeout
                : MetadataBundleConstants.DownloadTimeout);

        Uri bundleUri = BuildUri(hutaoEndpointsFactory.Create().MetadataBundle(cultureOptions.LocaleName));
        BundleDownloadResult result = await TryDownloadAsync(bundleUri, bundlePath, timeout.Token, token).ConfigureAwait(false);
        if (result.Path is not null)
        {
            span.SetData("metadata.bundle.downloaded", result.Downloaded);
            return result.Path;
        }

        span.SetData("metadata.bundle.downloaded", false);
        RemoveExpiredCache(bundlePath);
        return IsCacheFresh(bundlePath) ? bundlePath : null;
    }

    private static Uri BuildUri(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            ? uri
            : throw new ArgumentException($"Metadata URL is not absolute: '{url}'", nameof(url));
    }

    private async ValueTask<BundleDownloadResult> TryDownloadAsync(
        Uri uri,
        string destination,
        CancellationToken token,
        CancellationToken callerToken,
        bool allowConditionalRequest = true)
    {
        string temp = $"{destination}{MetadataBundleConstants.TempFileSuffix}";
        string etagPath = $"{destination}{MetadataBundleConstants.ETagFileSuffix}";
        BackgroundActivityTask activity = backgroundActivityOptions.MetadataInitialization;

        try
        {
            HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create().SetRequestUri(uri).Get();
            using HttpRequestMessage request = builder.HttpRequestMessage;
            bool hasCachedBundle = IsCacheFresh(destination);
            EntityTagHeaderValue? cachedETag = hasCachedBundle && allowConditionalRequest ? ReadETag(etagPath) : null;
            if (cachedETag is not null)
            {
                request.Headers.IfNoneMatch.Add(cachedETag);
            }

            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.NotModified)
            {
                if (cachedETag is not null && hasCachedBundle && IsCacheFresh(destination))
                {
                    logger.LogDebug("Metadata bundle is unchanged according to its ETag");
                    await WriteETagAsync(etagPath, response.Headers.ETag ?? cachedETag, token).ConfigureAwait(false);

                    return new(destination, false);
                }

                if (allowConditionalRequest)
                {
                    logger.LogWarning("Metadata bundle server returned 304 without a usable local bundle; retrying unconditionally");
                    return await TryDownloadAsync(uri, destination, token, callerToken, allowConditionalRequest: false).ConfigureAwait(false);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Metadata bundle download failed with status {StatusCode}", (int)response.StatusCode);
                activity.Update(taskContext, SH.ServiceMetadataBundleDownloadFailed, false, false, false, false);
                return default;
            }

            long total = response.Content.Headers.ContentLength ?? 0;
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            activity.Update(
                taskContext,
                total > 0
                    ? SH.FormatServiceMetadataBundleDownloading(0, FormatSize(0), FormatSize(total))
                    : SH.FormatServiceMetadataBundleDownloadingIndeterminate(FormatSize(0)),
                false,
                false,
                true,
                total <= 0,
                0);

            long copied = 0;
            int lastPercent = -1;
            long lastUnknownChunk = -1;
            using (Stream source = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
            using (FileStream file = new(temp, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(81920);
                try
                {
                    int read;
                    while ((read = await source.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
                    {
                        copied += read;
                        if (copied > MetadataBundleConstants.MaxEncryptedBytes)
                        {
                            logger.LogWarning("Metadata bundle exceeds the maximum accepted size");
                            TryDelete(temp);
                            activity.Update(taskContext, SH.ServiceMetadataBundleInvalid, false, false, false, false);
                            return default;
                        }

                        await file.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);

                        if (total > 0)
                        {
                            int percent = (int)(copied * 100 / total);
                            if (percent != lastPercent)
                            {
                                lastPercent = percent;
                                activity.Update(
                                    taskContext,
                                    SH.FormatServiceMetadataBundleDownloading(percent, FormatSize(copied), FormatSize(total)),
                                    false,
                                    false,
                                    true,
                                    false,
                                    percent / 100D);
                            }
                        }
                        else
                        {
                            // No Content-Length: report the downloaded size every 256 KiB.
                            long chunk = copied >> 18;
                            if (chunk != lastUnknownChunk)
                            {
                                lastUnknownChunk = chunk;
                                activity.Update(
                                    taskContext,
                                    SH.FormatServiceMetadataBundleDownloadingIndeterminate(FormatSize(copied)),
                                    false,
                                    false,
                                    true,
                                    true,
                                    0);
                            }
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            activity.Update(taskContext, SH.ServiceMetadataBundleValidating, false, false, true, true);
            if (!ValidateDownloadedBundle(temp, copied, token))
            {
                TryDelete(temp);
                activity.Update(taskContext, SH.ServiceMetadataBundleInvalid, false, false, false, false);
                return default;
            }

            // Validate first, replace second: a failed download never removes the usable bundle.
            if (!TryDelete(etagPath))
            {
                TryDelete(temp);
                return default;
            }

            File.Move(temp, destination, overwrite: true);
            if (response.Headers.ETag is { } responseETag)
            {
                await WriteETagAsync(etagPath, responseETag, token).ConfigureAwait(false);
            }

            activity.Update(taskContext, SH.ServiceMetadataDecoding, false, false, true, true);
            return new(destination, true);
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested && !XamlApplicationLifetime.ShutdownToken.IsCancellationRequested)
        {
            logger.LogWarning("Metadata bundle download timed out");
            TryDelete(temp);
            activity.Update(taskContext, SH.ServiceMetadataBundleDownloadFailed, false, false, false, false);
            return default;
        }
        catch (OperationCanceledException)
        {
            TryDelete(temp);
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Metadata bundle download failed");
            TryDelete(temp);
            activity.Update(taskContext, SH.ServiceMetadataBundleDownloadFailed, false, false, false, false);
            return default;
        }
    }

    private EntityTagHeaderValue? ReadETag(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            string value = File.ReadAllText(path).Trim();
            if (EntityTagHeaderValue.TryParse(value, out EntityTagHeaderValue? etag))
            {
                return etag;
            }

            logger.LogWarning("Metadata bundle ETag cache is invalid; refreshing without a conditional request");
            TryDelete(path);
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Failed to read metadata bundle ETag cache");
            return null;
        }
    }

    private bool IsCacheFresh(string bundlePath)
    {
        if (!File.Exists(bundlePath))
        {
            return false;
        }

        string etagPath = $"{bundlePath}{MetadataBundleConstants.ETagFileSuffix}";
        string freshnessPath = File.Exists(etagPath) && ReadETag(etagPath) is not null ? etagPath : bundlePath;
        try
        {
            TimeSpan age = DateTime.UtcNow - File.GetLastWriteTimeUtc(freshnessPath);
            return age >= TimeSpan.Zero && age < MetadataBundleConstants.MaxCacheAge;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Failed to determine metadata bundle cache age");
            return false;
        }
    }

    private void RemoveExpiredCache(string bundlePath)
    {
        string etagPath = $"{bundlePath}{MetadataBundleConstants.ETagFileSuffix}";
        if (!File.Exists(bundlePath))
        {
            TryDelete(etagPath);
            return;
        }

        if (!IsCacheFresh(bundlePath))
        {
            logger.LogInformation("Metadata bundle cache expired; removing it");
            TryDelete(bundlePath);
            TryDelete(etagPath);
        }
    }

    private async ValueTask WriteETagAsync(string path, EntityTagHeaderValue etag, CancellationToken token)
    {
        string temp = $"{path}{MetadataBundleConstants.TempFileSuffix}";
        try
        {
            await File.WriteAllTextAsync(temp, etag.ToString(), token).ConfigureAwait(false);
            File.Move(temp, path, overwrite: true);
        }
        catch (OperationCanceledException)
        {
            TryDelete(temp);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Failed to persist metadata bundle ETag; the next refresh will be unconditional");
            TryDelete(temp);
        }
    }

    private bool ValidateDownloadedBundle(string path, long size, CancellationToken token)
    {
        if (size <= 0)
        {
            logger.LogWarning("Metadata bundle download is empty");
            return false;
        }

        try
        {
            MetadataBundle bundle = MetadataBundleCodec.Read<MetadataBundle>(path, MetadataProtoModel.Instance, token);
            if (bundle.Languages.Length is not 1 || !string.Equals(bundle.Languages[0].Language, cultureOptions.LocaleName, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Metadata bundle language mismatch");
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Metadata bundle could not be decoded");
            return false;
        }
    }

    private static string FormatSize(long bytes)
    {
        const long KiB = 1024;
        const long MiB = KiB * 1024;
        const long GiB = MiB * 1024;

        return bytes >= GiB ? $"{bytes / (double)GiB:0.0} GiB"
            : bytes >= MiB ? $"{bytes / (double)MiB:0.0} MiB"
            : bytes >= KiB ? $"{bytes / (double)KiB:0.0} KiB"
            : $"{bytes} B";
    }

    private bool TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Failed to delete a metadata bundle cache file");
            return false;
        }
    }

    private readonly record struct BundleDownloadResult(string? Path, bool Downloaded);
}
