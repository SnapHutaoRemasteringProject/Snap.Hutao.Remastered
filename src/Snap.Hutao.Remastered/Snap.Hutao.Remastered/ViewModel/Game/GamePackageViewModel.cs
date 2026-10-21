// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Game;
using Snap.Hutao.Remastered.Service.Game.Configuration;
using Snap.Hutao.Remastered.Service.Game.FileSystem;
using Snap.Hutao.Remastered.Service.Game.Package;
using Snap.Hutao.Remastered.Service.Game.Package.Advanced;
using Snap.Hutao.Remastered.Service.Game.Package.Advanced.Model;
using Snap.Hutao.Remastered.Service.Game.Scheme;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Web.Hoyolab.HoyoPlay.Connect.Branch;
using Snap.Hutao.Remastered.Web.Hoyolab.HoyoPlay.Connect.ChannelSDK;
using System.IO;

namespace Snap.Hutao.Remastered.ViewModel.Game;

[BindableCustomPropertyProvider]
[Service(ServiceLifetime.Singleton)]
public sealed partial class GamePackageViewModel : Abstraction.ViewModel
{
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly GameInstallationService gameInstallationService;
    private readonly AsyncLock reloadLock = new();
    private readonly IGamePackageService gamePackageService;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly LaunchGameShared launchGameShared;
    private readonly IServiceProvider serviceProvider;
    private readonly IHoyoPlayService hoyoPlayService;
    private readonly LaunchOptions launchOptions;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;
    private PredownloadProgressTracker? activePredownloadProgress;
    private PredownloadProgressSnapshot cachedPredownloadProgress;

    [GeneratedConstructor]
    public partial GamePackageViewModel(IServiceProvider serviceProvider);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VersionSourcesText), nameof(InstallationStatusText), nameof(HasInstallationWarning), nameof(VerifyOperationLabel), nameof(CanOperate))]
    public partial GameInstallationInfo? InstallationInfo { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRefreshing), nameof(IsUpdateAvailable), nameof(IsPredownloadButtonEnabled), nameof(HasInstallationWarning), nameof(CanOperate))]
    public partial bool IsRefreshing { get; private set; }

    public bool IsNotRefreshing { get => !IsRefreshing; }
    public bool CanOperate { get => !IsRefreshing && InstallationInfo is { Channel.ErrorKind: ChannelOptionsErrorKind.None }; }
    public string VerifyOperationLabel { get => InstallationInfo is { InstalledVersion: null } ? SH.ViewPageGameVerifyAndRepairLatest : SH.ViewPageLaunchGamePackageOperationVerify; }
    public bool HasInstallationWarning { get => !IsRefreshing && InstallationInfo is { HasVersionProblem: true }; }
    public string VersionSourcesText { get => InstallationInfo is { } info ? SH.FormatViewModelGameInstallationSources(FormatVersion(info.ConfigVersion, info.ConfigStatus), FormatVersion(info.ScriptVersion, info.ScriptStatus)) : string.Empty; }
    public string InstallationStatusText
    {
        get => InstallationInfo switch
        {
            { HasVersionConflict: true } => SH.ViewModelGameInstallationConflict,
            { ConfigStatus: GameVersionFileStatus.Invalid or GameVersionFileStatus.Unreadable } or { ScriptStatus: GameVersionFileStatus.Invalid or GameVersionFileStatus.Unreadable } => SH.ViewModelGameInstallationInvalid,
            { HasVersionProblem: true } => SH.ViewModelGameInstallationMissingSource,
            _ => string.Empty,
        };
    }

    private static string FormatVersion(Version? version, GameVersionFileStatus status)
    {
        return version?.ToString() ?? (status switch
        {
            GameVersionFileStatus.Missing => SH.ViewModelGameInstallationSourceMissing,
            GameVersionFileStatus.Unreadable => SH.ViewModelGameInstallationSourceUnreadable,
            _ => SH.ViewModelGameInstallationSourceInvalid,
        });
    }

    private void ApplyInstallation(GameInstallationInfo? info)
    {
        InstallationInfo = info;
        LocalVersion = info?.InstalledVersion;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LocalVersionText), nameof(IsUpdateAvailable))]
    public partial Version? LocalVersion { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemoteVersionText), nameof(IsUpdateAvailable))]
    public partial Version? RemoteVersion { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreVersionText), nameof(IsPredownloadButtonEnabled))]
    public partial Version? PreVersion { get; set; }

    public string LocalVersionText { get => LocalVersion is null ? "Unknown" : SH.FormatViewModelGamePackageLocalVersion(LocalVersion); }

    public string RemoteVersionText { get => SH.FormatViewModelGamePackageRemoteVersion(RemoteVersion); }

    public string PreVersionText { get => SH.FormatViewModelGamePackagePreVersion(PreVersion); }

    public bool IsUpdateAvailable { get => !IsRefreshing && LocalVersion is not null && RemoteVersion is not null && LocalVersion < RemoteVersion; }

    public bool IsPredownloadButtonEnabled
    {
        get
        {
            if (IsRefreshing || LocalVersion is null || PreVersion is null)
            {
                return false;
            }

            if (LocalVersion >= PreVersion)
            {
                return false;
            }

            const string LockTrace = $"{nameof(GamePackageViewModel)}.{nameof(IsPredownloadButtonEnabled)}";
            if (launchOptions.TryGetGameFileSystem(LockTrace, out IGameFileSystem? gameFileSystem) is not GameFileSystemErrorKind.None)
            {
                return false;
            }

            using (gameFileSystem)
            {
                // IsPredownloadFinished also TryGetGameFileSystem
                return !IsPredownloadFinished;
            }
        }
    }

    public bool IsPredownloadFinished
    {
        get
        {
            PredownloadProgressSnapshot info = GetPredownloadProgressInfo();
            return info.Finished && info.DownloadedBlocks == info.TotalBlocks;
        }
    }

    public int PredownloadProgress
    {
        get => GetPredownloadProgressInfo().Percent;
    }

    public bool IsPredownloadInProgress
    {
        get
        {
            PredownloadProgressSnapshot info = GetPredownloadProgressInfo();
            return info.TotalBlocks > 0 && !info.Finished;
        }
    }

    public string PredownloadProgressText { get => $"{PredownloadProgress}%"; }

    public async ValueTask ReloadAsync()
    {
        bool result = await LoadOverrideAsync(CancellationToken).ConfigureAwait(false);
        await taskContext.SwitchToMainThreadAsync();
        IsInitialized = result;
    }

    [Command("RefreshInstallationCommand")]
    private async Task RefreshInstallationAsync()
    {
        try { await ReloadAsync().ConfigureAwait(false); }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested) { }
        catch (Exception ex) { messenger.Send(InfoBarMessage.Error(ex)); }
    }

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        using (await reloadLock.LockAsync().ConfigureAwait(false))
        {
            await taskContext.SwitchToMainThreadAsync();
            IsRefreshing = true;
            ApplyInstallation(null);
            RemoteVersion = null;
            PreVersion = null;
            try
            {
                GameInstallationInfo? installation = await gameInstallationService.RefreshAsync(token).ConfigureAwait(false);
                await taskContext.SwitchToMainThreadAsync();
                token.ThrowIfCancellationRequested();
                ApplyInstallation(installation);
                if (installation is null) { return false; }
                LaunchScheme? scheme = launchGameShared.GetCurrentLaunchSchemeFromConfigurationFile();
                if (scheme is null) { return false; }
                GameBranch? branch = await GetCurrentGameBranchAsync(scheme).ConfigureAwait(false);
                await taskContext.SwitchToMainThreadAsync();
                token.ThrowIfCancellationRequested();
                if (!string.Equals(installation.GamePath, launchOptions.GamePathEntry.Value?.Path, StringComparison.OrdinalIgnoreCase)) { ApplyInstallation(null); return false; }
                if (branch is null) { return true; } // Local inspection does not depend on the network.
                (BranchWrapper remote, BranchWrapper? pre) = LocalSetting.Get(SettingKeys.TreatPredownloadAsMain, false)
                    ? (branch.PreDownload ?? branch.Main, null) : (branch.Main, branch.PreDownload);
                RemoteVersion = Version.TryParse(remote.Tag, out Version? latest) ? latest : null;
                PreVersion = Version.TryParse(pre?.Tag, out Version? next) ? next : null;
                return true;
            }
            finally
            {
                await taskContext.SwitchToMainThreadAsync();
                IsRefreshing = false;
            }
        }
    }

    [Command("StartCommand")]
    private async Task StartAsync(string? operation)
    {
        if (!CanOperate) { return; }
        try { await StartCoreAsync(operation).ConfigureAwait(false); }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested) { }
        catch (Exception ex) { messenger.Send(InfoBarMessage.Error(ex)); }
        finally
        {
            await RefreshInstallationAsync().ConfigureAwait(false);
        }
    }

    private async Task StartCoreAsync(string? operation)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Start operation", "GamePackageViewModel.Command", [("operation", operation ?? "<null>")]));

        if (!IsInitialized || IsRefreshing)
        {
            return;
        }

        if (!Enum.TryParse(operation, out GamePackageOperationKind operationKind))
        {
            return;
        }

        if (launchGameShared.GetCurrentLaunchSchemeFromConfigurationFile() is not { } currentScheme)
        {
            return;
        }

        const string LockTrace = $"{nameof(GamePackageViewModel)}.{nameof(StartAsync)}";
        if (launchOptions.TryGetGameFileSystem(LockTrace, out IGameFileSystem? gameFileSystem) is not GameFileSystemErrorKind.None)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(gameFileSystem);
        using (gameFileSystem)
        {
            GameInstallationInfo installation = gameInstallationService.Read(gameFileSystem);
            await taskContext.SwitchToMainThreadAsync();
            ApplyInstallation(installation);
            if (await GetCurrentGameBranchAsync(currentScheme).ConfigureAwait(false) is not { } branch)
            {
                return;
            }

            Version? manifestVersion = installation.InstalledVersion;
            if (manifestVersion is null && operationKind is GamePackageOperationKind.Verify)
            {
                BranchWrapper repairTarget = LocalSetting.Get(SettingKeys.TreatPredownloadAsMain, false) ? branch.PreDownload ?? branch.Main : branch.Main;
                Version.TryParse(repairTarget.Tag, out manifestVersion);
            }
            if (manifestVersion is null)
            {
                messenger.Send(InfoBarMessage.Warning(installation.HasVersionConflict ? SH.ViewModelGameInstallationConflict : SH.ViewModelGameInstallationInvalid));
                return;
            }
            SophonDecodedBuilds? builds = await GetSophonDecodedBuildsAsync(operationKind, branch, gameFileSystem, manifestVersion).ConfigureAwait(false);
            if (builds is null) { return; }
            GameChannelSDK? sdk = await GetCurrentGameChannelSDKAsync(currentScheme).ConfigureAwait(false);
            PredownloadProgressTracker? tracker = operationKind is GamePackageOperationKind.Predownload ? new() : null;
            GamePackageOperationContext context = new(serviceProvider, operationKind, gameFileSystem)
            {
                InstallationAtPreparation = installation,
                LocalBuild = builds.LocalBuild,
                RemoteBuild = builds?.RemoteBuild,
                PatchBuild = builds?.PatchBuild,
                GameChannelSDK = sdk,
                ProgressChanged = tracker is null ? null : report => OnPredownloadProgress(tracker, report),
            };

            if (tracker is not null)
            {
                await taskContext.SwitchToMainThreadAsync();
                activePredownloadProgress = tracker;
            }

            try
            {
                if (!await gamePackageService.ExecuteOperationAsync(context).ConfigureAwait(false))
                {
                    return;
                }
                if (operationKind is GamePackageOperationKind.Verify && installation.HasVersionProblem)
                {
                    // The chosen manifest has now been fully checked/repaired.
                    GameInstallationMetadataRepair.Commit(gameFileSystem.GameConfigurationFilePath, gameFileSystem.ScriptVersionFilePath, manifestVersion);
                }
            }
            finally
            {
                if (tracker is not null)
                {
                    await taskContext.SwitchToMainThreadAsync();
                    if (ReferenceEquals(activePredownloadProgress, tracker))
                    {
                        cachedPredownloadProgress = tracker.Snapshot;
                        activePredownloadProgress = null;
                        NotifyPredownloadStatusChanged();
                    }
                }
            }
        }

    }

    private void OnPredownloadProgress(PredownloadProgressTracker tracker, GamePackageOperationReport report)
    {
        if (XamlApplicationLifetime.Exiting || !ReferenceEquals(activePredownloadProgress, tracker)) return;
        PredownloadProgressSnapshot previous = tracker.Snapshot;
        switch (report)
        {
            case GamePackageOperationReport.Reset reset:
                tracker.Reset(reset.DownloadTotalChunks, reset.DownloadTotalBytes);
                break;
            case GamePackageOperationReport.Download download:
                tracker.Download(download.BytesRead, download.Chunks);
                break;
            case GamePackageOperationReport.Finish { OperationKind: GamePackageOperationKind.Predownload }:
                tracker.Complete();
                break;
        }

        if (previous != tracker.Snapshot) NotifyPredownloadStatusChanged();
    }

    private PredownloadProgressSnapshot GetPredownloadProgressInfo()
    {
        // The operation owns the filesystem lease. Progress must not try to
        // reacquire it or repeatedly scan files from an ItemsControl getter.
        if (activePredownloadProgress is { } active) return active.Snapshot;
        const string LockTrace = $"{nameof(GamePackageViewModel)}.{nameof(GetPredownloadProgressInfo)}";
        if (launchOptions.TryGetGameFileSystem(LockTrace, out IGameFileSystem? gameFileSystem) is not GameFileSystemErrorKind.None)
        {
            return cachedPredownloadProgress;
        }

        ArgumentNullException.ThrowIfNull(gameFileSystem);
        using (gameFileSystem)
        {
            if (!File.Exists(gameFileSystem.PredownloadStatusFilePath))
            {
                return cachedPredownloadProgress = default;
            }

            try
            {
                if (JsonSerializer.Deserialize<PredownloadStatus>(File.ReadAllText(gameFileSystem.PredownloadStatusFilePath), jsonOptions) is { } status)
                {
                    if (PreVersion is null || status.Tag != PreVersion.ToString()) return cachedPredownloadProgress = default;
                    int downloadedBlocks = Math.Max(Directory.GetFiles(gameFileSystem.ChunksDirectory).Length - 1, 0);
                    return cachedPredownloadProgress = PredownloadProgressSnapshot.FromBlocks(status.Finished, downloadedBlocks, status.TotalBlocks);
                }
            }
            catch (IOException)
            {
                // The status file may be replaced while finishing an operation.
            }
            catch (JsonException)
            {
                // Retain the last valid snapshot during a partial status write.
            }
        }

        return cachedPredownloadProgress;
    }

    private void NotifyPredownloadStatusChanged()
    {
        OnPropertyChanged(nameof(IsPredownloadButtonEnabled));
        OnPropertyChanged(nameof(IsPredownloadFinished));
        OnPropertyChanged(nameof(PredownloadProgress));
        OnPropertyChanged(nameof(IsPredownloadInProgress));
        OnPropertyChanged(nameof(PredownloadProgressText));
    }

    private async ValueTask<SophonDecodedBuilds?> GetSophonDecodedBuildsAsync(GamePackageOperationKind operationKind, GameBranch branch, IGameFileSystem gameFileSystem, Version manifestVersion)
    {

        SophonDecodedBuild? localBuild;
        SophonDecodedBuild? remoteBuild;
        SophonDecodedPatchBuild? patchBuild;

        ContentDialog fetchManifestDialog = await contentDialogFactory
            .CreateForIndeterminateProgressAsync(SH.UIXamlViewSpecializedSophonProgressDefault)
            .ConfigureAwait(false);
        using (await contentDialogFactory.BlockAsync(fetchManifestDialog).ConfigureAwait(false))
        {
            try
            {
                BranchWrapper localBranch = operationKind is GamePackageOperationKind.Verify && LocalSetting.Get(SettingKeys.TreatPredownloadAsMain, false)
                    ? branch.PreDownload?.GetTaggedCopy(manifestVersion.ToString()) ?? branch.Main.GetTaggedCopy(manifestVersion.ToString())
                    : branch.Main.GetTaggedCopy(manifestVersion.ToString());
                localBuild = await gamePackageService.DecodeManifestsAsync(gameFileSystem, localBranch).ConfigureAwait(false);

                BranchWrapper? remoteBranch = operationKind is GamePackageOperationKind.Update && LocalSetting.Get(SettingKeys.TreatPredownloadAsMain, false)
                    ? branch.PreDownload ?? branch.Main
                    : operationKind is GamePackageOperationKind.Predownload ? branch.PreDownload : branch.Main;
                remoteBuild = await gamePackageService.DecodeManifestsAsync(gameFileSystem, remoteBranch).ConfigureAwait(false);

                patchBuild = await gamePackageService.DecodeDiffManifestsAsync(gameFileSystem, remoteBranch).ConfigureAwait(false);

                ArgumentNullException.ThrowIfNull(localBuild);
                ArgumentNullException.ThrowIfNull(remoteBuild);
            }
            catch (Exception ex)
            {
                messenger.Send(InfoBarMessage.Error(ex));
                return default;
            }
        }

        return new()
        {
            LocalBuild = localBuild,
            RemoteBuild = remoteBuild,
            PatchBuild = patchBuild,
        };
    }

    private async ValueTask<GameBranch?> GetCurrentGameBranchAsync(LaunchScheme launchScheme)
    {
        if (await hoyoPlayService.TryGetBranchesAsync(launchScheme).ConfigureAwait(false) is not (true, { } branchesWrapper))
        {
            return default;
        }

        if (branchesWrapper.GameBranches.FirstOrDefault(b => b.Game.Id == launchScheme.GameId) is not { } branch)
        {
            messenger.Send(InfoBarMessage.Error(SH.ViewModelGamePackageGetGameBranchFailed, SH.FormatViewModelGamePackageLocalLaunchScheme(launchScheme.DisplayName)));
            return default;
        }

        return branch;
    }

    private async ValueTask<GameChannelSDK?> GetCurrentGameChannelSDKAsync(LaunchScheme launchScheme)
    {
        if (await hoyoPlayService.TryGetChannelSDKsAsync(launchScheme).ConfigureAwait(false) is not (true, { } channelSDKsWrapper))
        {
            return default;
        }

        // Channel sdk can be null
        return channelSDKsWrapper.GameChannelSDKs.FirstOrDefault(sdk => sdk.Game.Id == launchScheme.GameId);
    }

    private sealed class SophonDecodedBuilds
    {
        public required SophonDecodedBuild? LocalBuild { get; init; }

        public required SophonDecodedBuild? RemoteBuild { get; init; }

        public required SophonDecodedPatchBuild? PatchBuild { get; init; }
    }

}
