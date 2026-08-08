// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Service.Hutao;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.UI.Xaml.View.Dialog;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using Snap.Hutao.Remastered.Web.Response;

namespace Snap.Hutao.Remastered.Service.Update;

[Service(ServiceLifetime.Singleton, typeof(IUpdateService))]
public sealed partial class UpdateService : IUpdateService
{
    private const string UpdaterFilename = "Snap.Hutao.Remastered.Deployment.exe";

    // Avoid injecting services directly
    private readonly IServiceProvider serviceProvider;

    [GeneratedConstructor]
    public partial UpdateService(IServiceProvider serviceProvider);

    public string? UpdateInfo { get; set; }

    public async ValueTask<CheckUpdateResult> CheckUpdateAsync(CancellationToken token = default)
    {
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            CheckUpdateResult checkUpdateResult = new();
            try
            {
                ITaskContext taskContext = scope.ServiceProvider.GetRequiredService<ITaskContext>();
                await taskContext.SwitchToBackgroundAsync();

                HutaoInfrastructureClient infrastructureClient = scope.ServiceProvider.GetRequiredService<HutaoInfrastructureClient>();
                HutaoResponse<HutaoPackageInformation> response = await infrastructureClient.GetHutaoVersionInformationAsync(token).ConfigureAwait(false);

                if (!ResponseValidator.TryValidate(response, scope.ServiceProvider, out HutaoPackageInformation? packageInformation))
                {
                    checkUpdateResult.Kind = CheckUpdateResultKind.VersionApiInvalidResponse;
                    return checkUpdateResult;
                }

                checkUpdateResult.Kind = CheckUpdateResultKind.UpdateAvailable;
                checkUpdateResult.PackageInformation = packageInformation;

                if (!LocalSetting.Get(SettingKeys.OverrideUpdateVersionComparison, false))
                {
                    // Launched in an updated version
                    if (HutaoRuntime.Version >= checkUpdateResult.PackageInformation.Version)
                    {
                        checkUpdateResult.Kind = CheckUpdateResultKind.AlreadyUpdated;
                        return checkUpdateResult;
                    }
                }

                if (checkUpdateResult.PackageInformation.Validation is not { Length: > 0 })
                {
                    checkUpdateResult.Kind = CheckUpdateResultKind.VersionApiInvalidSha256;
                }

                return checkUpdateResult;
            }
            finally
            {
                UpdateInfo = checkUpdateResult.Kind switch
                {
                    CheckUpdateResultKind.UpdateAvailable => SH.FormatViewModelSettingUpdateAvailable(checkUpdateResult.PackageInformation?.Version.ToString()),
                    CheckUpdateResultKind.AlreadyUpdated => SH.ViewModelSettingAlreadyUpdated,
                    CheckUpdateResultKind.VersionApiInvalidResponse or CheckUpdateResultKind.VersionApiInvalidSha256 => SH.ViewModelSettingCheckUpdateFailed,
                    _ => default,
                };
            }
        }
    }

    public async ValueTask TriggerUpdateAsync(CheckUpdateResult result, CancellationToken token = default)
    {
        if (result.Kind is not CheckUpdateResultKind.UpdateAvailable)
        {
            return;
        }

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            ICurrentXamlWindowReference currentXamlWindowReference = scope.ServiceProvider.GetRequiredService<ICurrentXamlWindowReference>();
            IContentDialogFactory contentDialogFactory = scope.ServiceProvider.GetRequiredService<IContentDialogFactory>();
            IMessenger messenger = scope.ServiceProvider.GetRequiredService<IMessenger>();

            if (currentXamlWindowReference.Window is null)
            {
                return;
            }

            try
            {
                ContentDialogResult installUpdateUserConsentResult = await contentDialogFactory
                    .CreateForConfirmCancelAsync(
                        SH.FormatViewTitleUpdatePackageAvailableTitle(result.PackageInformation?.Version),
                        SH.ViewTitileUpdatePackageAvailableContent,
                        ContentDialogButton.Primary)
                    .ConfigureAwait(false);

                if (installUpdateUserConsentResult is not ContentDialogResult.Primary)
                {
                    return;
                }

                DownloadSourceDialog sourceDialog = await contentDialogFactory
                    .CreateInstanceAsync<DownloadSourceDialog>(scope.ServiceProvider)
                    .ConfigureAwait(false);

                (bool isOk, DownloadSourceKind downloadSource) = await sourceDialog.GetDownloadSourceAsync().ConfigureAwait(false);

                if (!isOk)
                {
                    return;
                }

                if (downloadSource is DownloadSourceKind.MirrorChyan)
                {
                    MirrorChyanCdkDialog cdkDialog = await contentDialogFactory
                        .CreateInstanceAsync<MirrorChyanCdkDialog>(scope.ServiceProvider)
                        .ConfigureAwait(false);

                    (bool cdkOk, string cdk) = await cdkDialog.GetCdkAsync().ConfigureAwait(false);

                    if (!cdkOk)
                    {
                        return;
                    }

                    LocalSetting.Set(SettingKeys.MirrorChyanCdk, cdk);
                }

#if IS_ALPHA_BUILD
                if (result.PackageInformation?.Mirrors.SingleOrDefault() is { MirrorType: Web.Hutao.HutaoPackageMirrorType.Browser } mirror)
                {
                    await Windows.System.Launcher.LaunchUriAsync(mirror.Url.ToUri());
                }
#else
                await LaunchUpdaterAsync(downloadSource).ConfigureAwait(false);
#endif
            }
            catch (Exception ex)
            {
                // Access to the path '?' is denied.
                // 0x80070002 无法启动服务，原因可能是已被禁用或与其相关联的设备没有启动
                // The process cannot access the file '?' because it is being used by another process.
                // 0x80070005 Attempted to perform an unauthorized operation.
                messenger.Send(InfoBarMessage.Error(ex));
            }
        }
    }

    private async ValueTask LaunchUpdaterAsync(DownloadSourceKind downloadSource = DownloadSourceKind.Official)
    {
        string updaterTargetPath = HutaoRuntime.GetDataUpdateCacheDirectoryFile(UpdaterFilename);
        InstalledLocation.CopyFileFromApplicationUri($"ms-appx:///{UpdaterFilename}", updaterTargetPath);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            HutaoUserOptions hutaoUserOptions = scope.ServiceProvider.GetRequiredService<HutaoUserOptions>();

            CommandLineBuilder commandLineBuilder = new();
            commandLineBuilder.Append("--update");
            commandLineBuilder.Append("--installer-kind", RuntimeEnvironment.IsUnpackaged ? "Installer" : "Msix");
            if (hutaoUserOptions.IsLoggedIn)
            {
                commandLineBuilder.Append("--api-key", await hutaoUserOptions.GetAccessTokenAsync().ConfigureAwait(false));
            }

            commandLineBuilder.Append("--source", downloadSource.ToString());

            if (downloadSource is DownloadSourceKind.MirrorChyan)
            {
                string cdk = LocalSetting.Get(SettingKeys.MirrorChyanCdk, string.Empty);
                if (!string.IsNullOrEmpty(cdk))
                {
                    commandLineBuilder.Append("--cdk", cdk);
                }

                string resId = RuntimeEnvironment.IsUnpackaged ? "SnapHutaoRemastered" : "SnapHutaoRemastered_msix";
                commandLineBuilder.Append("--res-id", resId);
            }

            // The updater will request UAC permissions itself
            ProcessFactory.StartUsingShellExecute(commandLineBuilder.ToString(), updaterTargetPath);

            if (RuntimeEnvironment.IsUnpackaged)
            {
                // After launching the deployer in unpackaged mode, the installer needs to
                // replace the running executable files. Exit the main application to avoid
                // file-in-use conflicts during installation.
                ITaskContext taskContext = scope.ServiceProvider.GetRequiredService<ITaskContext>();
                App app = serviceProvider.GetRequiredService<App>();
                taskContext.InvokeOnMainThread(app.Exit);

                // base.Exit() may not fully terminate the process in unpackaged mode,
                // so force exit to guarantee the process terminates completely.
                SentrySdk.Flush();
                Environment.Exit(0);
            }
        }
    }
}