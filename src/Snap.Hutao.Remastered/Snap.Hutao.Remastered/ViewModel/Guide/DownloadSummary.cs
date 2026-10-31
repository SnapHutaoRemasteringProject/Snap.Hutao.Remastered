// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Factory.Progress;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Service.StaticResource;

namespace Snap.Hutao.Remastered.ViewModel.Guide;

/// <summary>
/// One row of the guide's static resource download list.
/// </summary>
/// <remarks>
/// The row owns the observable presentation state and maps a
/// <see cref="StaticResourceDownloadResult"/> to localized text and notifications. The transfer
/// itself is delegated to <see cref="StaticResourceDownloader"/>, so this type contains no HTTP,
/// retry, stall or archive code.
/// </remarks>
public sealed partial class DownloadSummary : ObservableObject
{
    private readonly StaticResourceDownloader downloader;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;
    private readonly IProgress<StreamCopyStatus> progress;

    public DownloadSummary(IServiceProvider serviceProvider, string fileName)
    {
        downloader = serviceProvider.GetRequiredService<StaticResourceDownloader>();
        taskContext = serviceProvider.GetRequiredService<ITaskContext>();
        messenger = serviceProvider.GetRequiredService<IMessenger>();
        progress = serviceProvider.GetRequiredService<IProgressFactory>().CreateForMainThread<StreamCopyStatus>(UpdateProgress);

        FileName = fileName;
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
        StaticResourceDownloadResult result = await downloader
            .DownloadAndExtractAsync(FileName, progress, externalToken)
            .ConfigureAwait(false);

        switch (result.Status)
        {
            case StaticResourceDownloadStatus.Success:
                await taskContext.SwitchToMainThreadAsync();
                ProgressValue = 1;
                Description = SH.ViewModelWelcomeDownloadSummaryComplete;
                StaticResource.Fulfill(FileName);
                return true;

            case StaticResourceDownloadStatus.Canceled:
                await SetDescriptionAsync(SH.ViewModelWelcomeDownloadSummarySkipped);
                return false;

            case StaticResourceDownloadStatus.Stalled:
                await SetDescriptionAsync(SH.ViewModelWelcomeDownloadSummaryStalled);
                return false;

            case StaticResourceDownloadStatus.UnsupportedContent:
                await SetDescriptionAsync(SH.ViewModelWelcomeDownloadSummaryContentTypeNotMatch);
                return false;

            case StaticResourceDownloadStatus.Failed:
                ReportFailure(result);
                await SetDescriptionAsync(SH.ViewModelWelcomeDownloadSummaryException);
                return false;

            default:
                throw HutaoException.InvalidOperation($"Unhandled static resource download status: {result.Status}.");
        }
    }

    private async ValueTask SetDescriptionAsync(string description)
    {
        await taskContext.SwitchToMainThreadAsync();
        Description = description;
    }

    private void ReportFailure(StaticResourceDownloadResult result)
    {
        string title = SH.ViewModelWelcomeDownloadSummaryException;

        if (result.Message is { } message)
        {
            messenger.Send(InfoBarMessage.Error(title, message));
        }
        else if (result.Exception is { } exception)
        {
            messenger.Send(InfoBarMessage.Error(title, exception));
        }
    }

    private void UpdateProgress(StreamCopyStatus status)
    {
        Description = $"{Converters.ToFileSizeString(status.BytesReadSinceCopyStart)}/{Converters.ToFileSizeString(status.TotalBytes)}";
        ProgressValue = status.TotalBytes is 0 ? 0 : (double)status.BytesReadSinceCopyStart / status.TotalBytes;
        Speed = string.Format(SH.ViewModelWelcomeDownloadSummarySpeed, Converters.ToFileSizeString(status.BytesReadSinceLastReport));
    }
}
