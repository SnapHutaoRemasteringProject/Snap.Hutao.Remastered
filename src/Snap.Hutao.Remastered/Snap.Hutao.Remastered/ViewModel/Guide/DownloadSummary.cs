// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Caching;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Factory.Progress;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Collections.Frozen;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Mime;

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

    public async ValueTask<bool> DownloadAndExtractAsync()
    {
        await taskContext.SwitchToMainThreadAsync();
        ProgressValue = 1;
        Description = SH.ViewModelWelcomeDownloadSummaryComplete;
        StaticResource.Fulfill(FileName);
        return true;
    }

    private void UpdateProgressStatus(StreamCopyStatus status)
    {
        Description = $"{Converters.ToFileSizeString(status.BytesReadSinceCopyStart)}/{Converters.ToFileSizeString(status.TotalBytes)}";
        ProgressValue = status.TotalBytes is 0 ? 0 : (double)status.BytesReadSinceCopyStart / status.TotalBytes;
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
