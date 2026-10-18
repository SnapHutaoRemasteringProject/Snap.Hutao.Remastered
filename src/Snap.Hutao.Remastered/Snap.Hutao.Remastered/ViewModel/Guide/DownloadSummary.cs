// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Caching;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Factory.Progress;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Collections.Frozen;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Mime;
using System.Text;

namespace Snap.Hutao.Remastered.ViewModel.Guide;

public sealed partial class DownloadSummary : ObservableObject
{
    private static readonly FrozenSet<string?> AllowedMediaTypes =
    [
        MediaTypeNames.Application.Octet,
        MediaTypeNames.Application.Zip,

        // Super hacking, we now upload zip files as images
        MediaTypeNames.Image.Jpeg,
    ];

    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly ITaskContext taskContext;
    private readonly IImageCache imageCache;
    private readonly HttpClient httpClient;
    private readonly IMessenger messenger;

    private readonly string fileUrl;
    private readonly IProgress<StreamCopyStatus> progress;

    public DownloadSummary(IServiceProvider serviceProvider, string fileName)
    {
        taskContext = serviceProvider.GetRequiredService<ITaskContext>();
        httpRequestMessageBuilderFactory = serviceProvider.GetRequiredService<IHttpRequestMessageBuilderFactory>();
        httpClient = serviceProvider.GetRequiredService<HttpClient>();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(HutaoRuntime.UserAgent);
        imageCache = serviceProvider.GetRequiredService<IImageCache>();
        messenger = serviceProvider.GetRequiredService<IMessenger>();

        FileName = fileName;

        fileUrl = StaticResourcesEndpoints.StaticZip(fileName);
        progress = serviceProvider.GetRequiredService<IProgressFactory>().CreateForMainThread<StreamCopyStatus>(UpdateProgressStatus);
    }

    public string FileName { get; }

    [ObservableProperty]
    public partial string Description { get; private set; } = SH.ViewModelWelcomeDownloadSummaryDefault;

    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    [ObservableProperty]
    public partial string Speed { get; private set; } = string.Empty;

    public async ValueTask<bool> DownloadAndExtractAsync(CancellationToken externalToken = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory
            .Create()
            .SetRequestUri(fileUrl)
            .SetStaticResourceControlHeaders()
            .Get();

        try
        {
            await builder.PrepareForRetryAsync(externalToken).ConfigureAwait(false);

            using (CancellationTokenSource stallCts = new())
            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, stallCts.Token))
            {
                CancellationToken token = linkedCts.Token;

                // Stall detection: monitor bytes progress every 1s, cancel if stalled for 10s
                long lastBytesRead = 0;
                int stallSeconds = 0;
                Task stallMonitorTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            await Task.Delay(1000, token).ConfigureAwait(false);
                            long currentBytes = Interlocked.Read(ref _bytesReadDuringCopy);
                            if (currentBytes == 0)
                            {
                                // Download hasn't started yet, keep waiting
                                continue;
                            }

                            if (currentBytes == Volatile.Read(ref lastBytesRead))
                            {
                                stallSeconds++;
                                if (stallSeconds >= 10)
                                {
                                    await taskContext.SwitchToMainThreadAsync();
                                    Description = SH.ViewModelWelcomeDownloadSummaryStalled;
                                    stallCts.Cancel();
                                    break;
                                }
                            }
                            else
                            {
                                stallSeconds = 0;
                                Volatile.Write(ref lastBytesRead, currentBytes);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Token was cancelled, exit gracefully
                    }
                },
                token);

                int retryTimes = 0;
                while (retryTimes++ < 3)
                {
                    token.ThrowIfCancellationRequested();

                    builder.RecreateRequest();

                    TimeSpan delay = default;
                    using (HttpRequestMessage message = builder.HttpRequestMessage)
                    {
                        using (HttpResponseMessage response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode();

                            if (!AllowedMediaTypes.Contains(response.Content.Headers.ContentType?.MediaType))
                            {
                                await taskContext.SwitchToMainThreadAsync();
                                Description = SH.ViewModelWelcomeDownloadSummaryContentTypeNotMatch;
                            }
                            else
                            {
                                long contentLength = response.Content.Headers.ContentLength ?? 0;
                                using (Stream content = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
                                {
                                    using (TempFileStream tempStream = new(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                    {
                                        using (StreamCopyWorker worker = new(content, tempStream, contentLength))
                                        {
                                            await worker.CopyAsync(progress, token).ConfigureAwait(false);
                                        }

                                        token.ThrowIfCancellationRequested();

                                        await ExtractFilesAsync(tempStream).ConfigureAwait(false);

                                        await taskContext.SwitchToMainThreadAsync();
                                        ProgressValue = 1;
                                        Description = SH.ViewModelWelcomeDownloadSummaryComplete;
                                        StaticResource.Fulfill(FileName);
                                        return true;
                                    }
                                }
                            }

                            if (response.Headers.RetryAfter?.Delta is { } retryAfter)
                            {
                                delay = retryAfter;
                            }
                        }
                    }

                    await Task.Delay(delay, token).ConfigureAwait(false);
                }

                return false;
            }
        }
        catch (OperationCanceledException)
        {
            await taskContext.SwitchToMainThreadAsync();
            Description = SH.ViewModelWelcomeDownloadSummarySkipped;
            return false;
        }
        catch (Exception ex)
        {
            if (ex is not (IOException or UnauthorizedAccessException) &&
                HttpRequestExceptionHandling.TryHandle(builder, ex, out StringBuilder message))
            {
                messenger.Send(InfoBarMessage.Error(SH.ViewModelWelcomeDownloadSummaryException, message.ToString()));
            }
            else
            {
                // SSL certificate not trusted: The decryption operation failed, see inner exception. -> 无法解密指定的数据。
                messenger.Send(InfoBarMessage.Error(SH.ViewModelWelcomeDownloadSummaryException, ex));
            }

            await taskContext.SwitchToMainThreadAsync();
            Description = SH.ViewModelWelcomeDownloadSummaryException;
            return false;
        }
    }

    private long _bytesReadDuringCopy;

    private void UpdateProgressStatus(StreamCopyStatus status)
    {
        _bytesReadDuringCopy = status.BytesReadSinceCopyStart;
        Description = $"{Converters.ToFileSizeString(status.BytesReadSinceCopyStart)}/{Converters.ToFileSizeString(status.TotalBytes)}";
        ProgressValue = status.TotalBytes is 0 ? 0 : (double)status.BytesReadSinceCopyStart / status.TotalBytes;
        Speed = string.Format(SH.ViewModelWelcomeDownloadSummarySpeed, Converters.ToFileSizeString(status.BytesReadSinceLastReport));
    }

    private async ValueTask ExtractFilesAsync(Stream stream)
    {
        using (ZipArchive archive = await ZipArchive.CreateAsync(stream, ZipArchiveMode.Read, false, default))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string destPath = imageCache.GetFileFromCategoryAndName(FileName, entry.FullName);

                try
                {
                    await entry.ExtractToFileAsync(destPath, true).ConfigureAwait(false);
                }
                catch
                {
                    // Ignored
                }
            }
        }
    }
}
