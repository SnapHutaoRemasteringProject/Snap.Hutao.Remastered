// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Service.Game.Package.Advanced;
using System.Collections.Frozen;
using System.Diagnostics;

namespace Snap.Hutao.Remastered.ViewModel.Game;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Scoped)]
public sealed partial class GamePackageOperationViewModel : Abstraction.ViewModel
{
    private const string ZeroBytesPerSecondSpeed = "0 byte/s";
    private const string UnknownRemainingTime = "99:59:59";

    private static readonly TimeSpan ProgressTimeout = TimeSpan.FromSeconds(5);

    private readonly FrozenDictionary<GamePackageOperationReportKind, Lock> syncRoots = WinRTAdaptive.ToFrozenDictionary(
    [
        KeyValuePair.Create(GamePackageOperationReportKind.Download, new Lock()),
        KeyValuePair.Create(GamePackageOperationReportKind.Install, new Lock()),
    ]);

    private readonly ILogger<GamePackageOperationViewModel> logger;
    private readonly IGamePackageService gamePackageService;
    private readonly ITaskContext taskContext;

    private GamePackageOperationContext? operationContext;
    private long bytesDownloadedSinceLastUpdate;
    private long totalBytesDownloaded;
    private long bytesDownloadedLastRefreshTime;
    private long bytesInstalledSinceLastUpdate;
    private long totalBytesInstalled;
    private long bytesInstalledLastRefreshTime;
    private long downloadTotalBytes;
    private long installTotalBytes;
    private string? titleBeforeCancel;

    [GeneratedConstructor]
    public partial GamePackageOperationViewModel(IServiceProvider serviceProvider);

    public partial AppOptions AppOptions { get; }

    [ObservableProperty]
    public partial string Title { get; private set; } = SH.UIXamlViewSpecializedSophonProgressDefault;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCancel), nameof(CanClose), nameof(CanContinue))]
    public partial bool IsFinished { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCancel), nameof(CanClose), nameof(CanContinue))]
    public partial bool IsRetryableFailure { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCancel), nameof(CanClose), nameof(CanContinue))]
    public partial bool IsCanceling { get; private set; }

    [ObservableProperty]
    public partial long DownloadedBytes { get; private set; }

    [ObservableProperty]
    public partial long DownloadTotalBytes { get; private set; }

    [ObservableProperty]
    public partial long DownloadSpeedBytesPerSecond { get; private set; }

    public int DownloadedChunks { get; private set; }

    [ObservableProperty]
    public partial int DownloadTotalChunks { get; private set; } = -1;

    public string DownloadFileName { get; private set; } = default!;

    [ObservableProperty]
    public partial string DownloadSpeed { get; private set; } = ZeroBytesPerSecondSpeed;

    [ObservableProperty]
    public partial string DownloadRemainingTime { get; private set; } = UnknownRemainingTime;

    [ObservableProperty]
    public partial long InstalledBytes { get; private set; }

    [ObservableProperty]
    public partial long InstallTotalBytes { get; private set; }

    [ObservableProperty]
    public partial long InstallSpeedBytesPerSecond { get; private set; }

    public int InstalledChunks { get; private set; }

    [ObservableProperty]
    public partial int InstallTotalChunks { get; private set; } = -1;

    public string InstallFileName { get; private set; } = default!;

    [ObservableProperty]
    public partial string InstallSpeed { get; private set; } = ZeroBytesPerSecondSpeed;

    [ObservableProperty]
    public partial string InstallRemainingTime { get; private set; } = UnknownRemainingTime;

    public bool CanCancel { get => !IsFinished && !IsRetryableFailure && !IsCanceling; }

    public bool CanClose { get => IsFinished || IsRetryableFailure || IsCanceling; }

    public bool CanContinue { get => IsCanceling || IsRetryableFailure; }

    public void SetOperationContext(GamePackageOperationContext context)
    {
        operationContext = context;
    }

    public void HandleProgressUpdate(GamePackageOperationReport status)
    {
        switch (status)
        {
            case GamePackageOperationReport.Reset reset:
                ResetProgress(reset);
                return;
            case GamePackageOperationReport.Update update:
                UpdateProgress(update);
                return;
            case GamePackageOperationReport.Finish finish:
                FinishProgress(finish);
                break;
            case GamePackageOperationReport.Abort abort:
                AbortProgress(abort);
                break;
            case GamePackageOperationReport.RetryableFailure failure:
                RetryableFailureProgress(failure);
                break;
        }
    }

    private void UpdateProgress(GamePackageOperationReport.Update update)
    {
        long current = Stopwatch.GetTimestamp();
        lock (syncRoots[update.Kind])
        {
            switch (update)
            {
                case GamePackageOperationReport.Download download:
                    {
                        UpdateDownloadProgress(download);
                        TimeSpan elapsedTime = Stopwatch.GetElapsedTime(bytesDownloadedLastRefreshTime);
                        if (elapsedTime.TotalMilliseconds < 1000)
                        {
                            return;
                        }

                        bytesDownloadedLastRefreshTime = current;
                        long bytesDownloadedPerSecond = (long)(bytesDownloadedSinceLastUpdate / elapsedTime.TotalSeconds);
                        DownloadSpeedBytesPerSecond = bytesDownloadedPerSecond;
                        DownloadSpeed = $"{Converters.ToFileSizeString(bytesDownloadedPerSecond),8}/s";
                        logger.LogInformation("Download Info: [{Bytes}KB|{Duration}|{Speed}]", bytesDownloadedSinceLastUpdate / 1024D, elapsedTime, DownloadSpeed);
                        DownloadRemainingTime = bytesDownloadedPerSecond is 0
                            ? UnknownRemainingTime
                            : $"{TimeSpan.FromSeconds((double)(downloadTotalBytes - totalBytesDownloaded) / bytesDownloadedPerSecond):hh\\:mm\\:ss}";

                        bytesDownloadedSinceLastUpdate = 0;
                    }

                    break;
                case GamePackageOperationReport.Install install:
                    {
                        UpdateInstallProgress(install);
                        TimeSpan elapsedTime = Stopwatch.GetElapsedTime(bytesInstalledLastRefreshTime);
                        if (elapsedTime.TotalMilliseconds < 1000)
                        {
                            return;
                        }

                        bytesInstalledLastRefreshTime = current;
                        long bytesInstalledPerSecond = (long)(bytesInstalledSinceLastUpdate / elapsedTime.TotalSeconds);
                        InstallSpeedBytesPerSecond = bytesInstalledPerSecond;
                        InstallSpeed = $"{Converters.ToFileSizeString(bytesInstalledPerSecond),8}/s";
                        InstallRemainingTime = bytesInstalledPerSecond is 0
                            ? UnknownRemainingTime
                            : $"{TimeSpan.FromSeconds((double)(installTotalBytes - totalBytesInstalled) / bytesInstalledPerSecond):hh\\:mm\\:ss}";

                        bytesInstalledSinceLastUpdate = 0;
                    }

                    break;
                default:
                    HutaoException.NotSupported();
                    break;
            }

            RefreshUI();
        }
    }

    private void UpdateDownloadProgress(GamePackageOperationReport.Download download)
    {
        totalBytesDownloaded += download.BytesRead;
        DownloadedBytes = totalBytesDownloaded;
        bytesDownloadedSinceLastUpdate += download.BytesRead;
        DownloadedChunks += download.Chunks;
        DownloadFileName = download.FileName;
    }

    private void UpdateInstallProgress(GamePackageOperationReport.Install install)
    {
        totalBytesInstalled += install.BytesRead;
        InstalledBytes = totalBytesInstalled;
        bytesInstalledSinceLastUpdate += install.BytesRead;
        InstalledChunks += install.Chunks;
        InstallFileName = install.FileName;
    }

    private void ResetProgress(GamePackageOperationReport.Reset reset)
    {
        DownloadedChunks = 0;
        InstalledChunks = 0;
        totalBytesDownloaded = 0;
        DownloadedBytes = 0;
        bytesDownloadedSinceLastUpdate = 0;
        bytesDownloadedLastRefreshTime = Stopwatch.GetTimestamp();
        totalBytesInstalled = 0;
        InstalledBytes = 0;
        bytesInstalledSinceLastUpdate = 0;
        bytesInstalledLastRefreshTime = Stopwatch.GetTimestamp();
        downloadTotalBytes = reset.DownloadTotalBytes;
        DownloadTotalBytes = reset.DownloadTotalBytes;
        installTotalBytes = reset.InstallTotalBytes;
        InstallTotalBytes = reset.InstallTotalBytes;
        DownloadTotalChunks = reset.DownloadTotalChunks;
        DownloadFileName = default!;
        InstallTotalChunks = reset.InstallTotalChunks;
        InstallFileName = default!;
        DownloadSpeedBytesPerSecond = 0;
        DownloadSpeed = ZeroBytesPerSecondSpeed;
        DownloadRemainingTime = UnknownRemainingTime;
        InstallSpeedBytesPerSecond = 0;
        InstallSpeed = ZeroBytesPerSecondSpeed;
        InstallRemainingTime = UnknownRemainingTime;
        IsRetryableFailure = false;
        Title = reset.Title;
        RefreshUI();
    }

    private void FinishProgress(GamePackageOperationReport.Finish finish)
    {
        Title = finish.OperationKind switch
        {
            GamePackageOperationKind.Install => SH.ViewModelGamePackageOperationCompleteInstall,
            GamePackageOperationKind.Verify => finish.Repaired ? SH.ViewModelGamePackageOperationCompleteRepair : SH.ViewModelGamePackageOperationSkipRepair,
            GamePackageOperationKind.Update => SH.ViewModelGamePackageOperationCompleteUpdate,
            GamePackageOperationKind.ExtractBlocks or GamePackageOperationKind.ExtractExecutable => "Extracted",
            GamePackageOperationKind.Predownload => SH.ViewModelGamePackageOperationCompletePredownload,
            _ => throw HutaoException.NotSupported(),
        };

        IsFinished = true;
        IsRetryableFailure = false;
        IsCanceling = false;
        RefreshUI();
    }

    private void AbortProgress(GamePackageOperationReport.Abort abort)
    {
        Title = SH.FormatViewModelGamePackageOperationAborted(abort.Reason);
        IsFinished = true;
        IsRetryableFailure = false;
        IsCanceling = false;
        RefreshUI();
    }

    private void RetryableFailureProgress(GamePackageOperationReport.RetryableFailure failure)
    {
        Title = failure.Reason;
        IsRetryableFailure = true;
        IsCanceling = false;
        DownloadSpeedBytesPerSecond = 0;
        DownloadSpeed = ZeroBytesPerSecondSpeed;
        DownloadRemainingTime = UnknownRemainingTime;
        InstallSpeedBytesPerSecond = 0;
        InstallSpeed = ZeroBytesPerSecondSpeed;
        InstallRemainingTime = UnknownRemainingTime;
        RefreshUI();
    }

    private void RefreshUI()
    {
        OnPropertyChanged(nameof(DownloadedChunks));
        OnPropertyChanged(nameof(InstalledChunks));
        OnPropertyChanged(nameof(DownloadFileName));
        OnPropertyChanged(nameof(InstallFileName));
    }

    [Command("PeriodicRefreshUICommand")]
    private async Task PeriodicRefreshUIAsync()
    {
        using (PeriodicTimer timer = new(TimeSpan.FromSeconds(5)))
        {
            do
            {
                Refresh();
                await timer.WaitForNextTickAsync(CancellationToken).ConfigureAwait(false);
            }
            while (!IsFinished && !CancellationToken.IsCancellationRequested);
        }

        void Refresh()
        {
            TimeSpan elapsedTimeSinceLastDownloadReport = Stopwatch.GetElapsedTime(bytesDownloadedLastRefreshTime);
            TimeSpan elapsedTimeSinceLastInstallReport = Stopwatch.GetElapsedTime(bytesInstalledLastRefreshTime);

            if (elapsedTimeSinceLastDownloadReport > ProgressTimeout)
            {
                taskContext.InvokeOnMainThread(() =>
                {
                    DownloadSpeedBytesPerSecond = 0;
                    DownloadSpeed = ZeroBytesPerSecondSpeed;
                    DownloadRemainingTime = UnknownRemainingTime;
                });
            }

            if (elapsedTimeSinceLastInstallReport > ProgressTimeout)
            {
                taskContext.InvokeOnMainThread(() =>
                {
                    InstallSpeedBytesPerSecond = 0;
                    InstallSpeed = ZeroBytesPerSecondSpeed;
                    InstallRemainingTime = UnknownRemainingTime;
                });
            }
        }
    }

    [Command("CancelCommand")]
    private async Task CancelAsync()
    {
        if (IsFinished || IsRetryableFailure || IsCanceling)
        {
            return;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Cancel", "GamePackageOperationViewModel.Command"));

        titleBeforeCancel = Title;
        IsCanceling = true;
        await gamePackageService.CancelOperationAsync().ConfigureAwait(true);
    }

    [Command("ContinueCommand")]
    private async Task ContinueAsync()
    {
        if (operationContext is null)
        {
            return;
        }

        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateUI("Continue", "GamePackageOperationViewModel.Command"));

        IsCanceling = false;
        IsRetryableFailure = false;
        Title = titleBeforeCancel ?? Title;
        await gamePackageService.ContinueOperationAsync().ConfigureAwait(true);
    }
}