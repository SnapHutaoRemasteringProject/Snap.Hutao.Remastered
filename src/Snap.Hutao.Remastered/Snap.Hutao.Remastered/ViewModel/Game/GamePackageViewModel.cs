// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Game;
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
    private readonly IGamePackageService gamePackageService;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly LaunchGameShared launchGameShared;
    private readonly IServiceProvider serviceProvider;
    private readonly IHoyoPlayService hoyoPlayService;
    private readonly LaunchOptions launchOptions;
    private readonly ITaskContext taskContext;
    private readonly IMessenger messenger;

    [GeneratedConstructor]
    public partial GamePackageViewModel(IServiceProvider serviceProvider);

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

    public bool IsUpdateAvailable { get => LocalVersion < RemoteVersion; }

    public bool IsPredownloadButtonEnabled
    {
        get
        {
            if (PreVersion is null)
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
            PredownloadProgressInfo info = GetPredownloadProgressInfo();
            return info.Finished && info.DownloadedBlocks == info.TotalBlocks;
        }
    }

    public int PredownloadProgress
    {
        get
        {
            PredownloadProgressInfo info = GetPredownloadProgressInfo();
            int progress = info.TotalBlocks is 0 ? 0 : (int)Math.Clamp((long)info.DownloadedBlocks * 100 / info.TotalBlocks, 0, 100);
            return info.Finished ? progress : Math.Min(progress, 99);
        }
    }

    public bool IsPredownloadInProgress
    {
        get
        {
            PredownloadProgressInfo info = GetPredownloadProgressInfo();
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

    protected override async ValueTask<bool> LoadOverrideAsync(CancellationToken token)
    {
        if (launchGameShared.GetCurrentLaunchSchemeFromConfigurationFile() is not { } launchScheme)
        {
            return false;
        }

        if (await GetCurrentGameBranchAsync(launchScheme).ConfigureAwait(false) is not { } branch)
        {
            return false;
        }

        await taskContext.SwitchToMainThreadAsync();

        (BranchWrapper remote, BranchWrapper? pre) = LocalSetting.Get(SettingKeys.TreatPredownloadAsMain, false)
            ? (branch.PreDownload ?? branch.Main, default)
            : (branch.Main, branch.PreDownload);

        (RemoteVersion, PreVersion) = (new(remote.Tag), pre is { Tag: { } preTag } ? new(preTag) : default);

        const string LockTrace = $"{nameof(GamePackageViewModel)}.{nameof(LoadOverrideAsync)}";
        if (launchOptions.TryGetGameFileSystem(LockTrace, out IGameFileSystem? gameFileSystem) is not GameFileSystemErrorKind.None)
        {
            return false;
        }

        ArgumentNullException.ThrowIfNull(gameFileSystem);
        using (gameFileSystem)
        {
            if (gameFileSystem.TryGetGameVersion(out string? localVersion))
            {
                _ = Version.TryParse(localVersion, out Version? version);
                LocalVersion = version;
            }

            if (!IsUpdateAvailable && PreVersion is null && File.Exists(gameFileSystem.PredownloadStatusFilePath))
            {
                File.Delete(gameFileSystem.PredownloadStatusFilePath);
            }
        }

        return true;
    }

    [Command("StartCommand")]
    private async Task StartAsync(string? operation)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory2.CreateUI("Start operation", "GamePackageViewModel.Command", [("operation", operation ?? "<null>")]));

        if (!IsInitialized)
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
            if (await GetCurrentGameBranchAsync(currentScheme).ConfigureAwait(false) is not { } branch)
            {
                return;
            }

            SophonDecodedBuilds? builds = await GetSophonDecodedBuildsAsync(operationKind, branch, gameFileSystem).ConfigureAwait(false);
            GameChannelSDK? sdk = await GetCurrentGameChannelSDKAsync(currentScheme).ConfigureAwait(false);
            GamePackageOperationContext context = new(serviceProvider, operationKind, gameFileSystem)
            {
                LocalBuild = builds?.LocalBuild,
                RemoteBuild = builds?.RemoteBuild,
                PatchBuild = builds?.PatchBuild,
                GameChannelSDK = sdk,
            };

            if (!await gamePackageService.ExecuteOperationAsync(context).ConfigureAwait(false))
            {
                // Operation canceled
                if (operationKind is GamePackageOperationKind.Predownload)
                {
                    await taskContext.SwitchToMainThreadAsync();
                    NotifyPredownloadStatusChanged();
                }

                return;
            }
        }

        await taskContext.SwitchToMainThreadAsync();

        switch (operationKind)
        {
            case GamePackageOperationKind.Verify:
                break;
            case GamePackageOperationKind.Update:
                LocalVersion = RemoteVersion;
                break;
            case GamePackageOperationKind.Predownload:
                NotifyPredownloadStatusChanged();
                break;
        }
    }

    private PredownloadProgressInfo GetPredownloadProgressInfo()
    {
        const string LockTrace = $"{nameof(GamePackageViewModel)}.{nameof(GetPredownloadProgressInfo)}";
        if (launchOptions.TryGetGameFileSystem(LockTrace, out IGameFileSystem? gameFileSystem) is not GameFileSystemErrorKind.None)
        {
            return default;
        }

        ArgumentNullException.ThrowIfNull(gameFileSystem);
        using (gameFileSystem)
        {
            if (!File.Exists(gameFileSystem.PredownloadStatusFilePath))
            {
                return default;
            }

            if (JsonSerializer.Deserialize<PredownloadStatus>(File.ReadAllText(gameFileSystem.PredownloadStatusFilePath), jsonOptions) is { } status)
            {
                int downloadedBlocks = Math.Max(Directory.GetFiles(gameFileSystem.ChunksDirectory).Length - 1, 0);
                return new(status.Finished, downloadedBlocks, status.TotalBlocks);
            }
        }

        return default;
    }

    private void NotifyPredownloadStatusChanged()
    {
        OnPropertyChanged(nameof(IsPredownloadButtonEnabled));
        OnPropertyChanged(nameof(IsPredownloadFinished));
        OnPropertyChanged(nameof(PredownloadProgress));
        OnPropertyChanged(nameof(IsPredownloadInProgress));
        OnPropertyChanged(nameof(PredownloadProgressText));
    }

    private async ValueTask<SophonDecodedBuilds?> GetSophonDecodedBuildsAsync(GamePackageOperationKind operationKind, GameBranch branch, IGameFileSystem gameFileSystem)
    {
        ArgumentNullException.ThrowIfNull(LocalVersion);

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
                    ? branch.PreDownload?.GetTaggedCopy(LocalVersion.ToString()) ?? branch.Main.GetTaggedCopy(LocalVersion.ToString())
                    : branch.Main.GetTaggedCopy(LocalVersion.ToString());
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

    private readonly record struct PredownloadProgressInfo(bool Finished, int DownloadedBlocks, int TotalBlocks);
}
