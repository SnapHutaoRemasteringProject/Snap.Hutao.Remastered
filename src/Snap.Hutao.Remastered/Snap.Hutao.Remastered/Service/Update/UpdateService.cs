// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Diagnostics;
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
using System.IO;
using System.Net.Http;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Snap.Hutao.Remastered.Service.Update;

[Service(ServiceLifetime.Singleton, typeof(IUpdateService))]
public sealed partial class UpdateService : IUpdateService
{
    private const string AppInstallerHost = "package.snaphutaorp.org";
    private const string PublishPackageName = "E8B6E2B3-D2A0-4435-A81D-2A16AAF405C8";
    private const string UpdaterFilename = "Snap.Hutao.Remastered.Deployment.exe";

    // Avoid injecting services directly
    private readonly IServiceProvider serviceProvider;

    [GeneratedConstructor]
    public partial UpdateService(IServiceProvider serviceProvider);

    public string? UpdateInfo { get; set; }

    public async ValueTask<CheckUpdateResult> CheckUpdateAsync(CancellationToken token = default)
    {
        using SentrySpanScope span = SentrySpanScope.StartOperation("update.check", "update");
        span.SetData("update.current_version", $"{HutaoRuntime.Version}");

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            CheckUpdateResult checkUpdateResult = new();
            try
            {
                ITaskContext taskContext = scope.ServiceProvider.GetRequiredService<ITaskContext>();
                await taskContext.SwitchToBackgroundAsync();

                if (RuntimeEnvironment.IsPackaged)
                {
                    Uri? descriptorUri = GetPackagedAppInstallerUri();
                    if (descriptorUri is null)
                    {
                        checkUpdateResult.Kind = CheckUpdateResultKind.AppInstallerUnavailable;
                        return checkUpdateResult;
                    }

                    Version? remoteVersion = null;
                    try
                    {
                        remoteVersion = await GetRemoteAppInstallerVersionAsync(descriptorUri, token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is HttpRequestException or IOException or System.Xml.XmlException
                        || (ex is OperationCanceledException && !token.IsCancellationRequested))
                    {
                        SentrySdk.CaptureException(ex);
                    }

                    if (remoteVersion is not null)
                    {
                        checkUpdateResult.PackageInformation = new HutaoPackageInformation { Version = remoteVersion };
                        checkUpdateResult.Kind = !LocalSetting.Get(SettingKeys.OverrideUpdateVersionComparison, false)
                            && HutaoRuntime.Version >= remoteVersion
                            ? CheckUpdateResultKind.AlreadyUpdated
                            : CheckUpdateResultKind.UpdateAvailable;
                        return checkUpdateResult;
                    }

                    // Only public packages can fall back to the public release metadata and Mirror Chyan.
                    if (!IsPublishPackage())
                    {
                        checkUpdateResult.Kind = CheckUpdateResultKind.VersionApiInvalidResponse;
                        return checkUpdateResult;
                    }
                }

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

                return checkUpdateResult;
            }
            finally
            {
                span.SetData("update.result", checkUpdateResult.Kind.ToString());
                UpdateInfo = checkUpdateResult.Kind switch
                {
                    CheckUpdateResultKind.UpdateAvailable => SH.FormatViewModelSettingUpdateAvailable(checkUpdateResult.PackageInformation?.Version.ToString()),
                    CheckUpdateResultKind.AlreadyUpdated => SH.ViewModelSettingAlreadyUpdated,
                    CheckUpdateResultKind.VersionApiInvalidResponse or CheckUpdateResultKind.VersionApiInvalidSha256 => SH.ViewModelSettingCheckUpdateFailed,
                    CheckUpdateResultKind.AppInstallerUnavailable => SH.ServiceUpdateAppInstallerUnavailable,
                    _ => default,
                };
            }
        }
    }

    public async ValueTask TriggerUpdateAsync(CheckUpdateResult result, bool isAutomatic = false, CancellationToken token = default)
    {
        if (result.Kind is CheckUpdateResultKind.AppInstallerUnavailable)
        {
            using IServiceScope unavailableScope = serviceProvider.CreateScope();
            unavailableScope.ServiceProvider.GetRequiredService<IMessenger>()
                .Send(InfoBarMessage.Error(SH.ServiceUpdateAppInstallerUnavailable));
            return;
        }

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
                // The user already skipped this version once, don't prompt again on automatic checks.
                // A manual check from the setting page always shows the dialog, so the user can
                // still choose to update.
                if (isAutomatic && IsVersionSkipped(result.PackageInformation?.Version))
                {
                    return;
                }

                ContentDialogResult installUpdateUserConsentResult = await contentDialogFactory
                    .CreateForConfirmCancelWithSecondaryAsync(
                        SH.FormatViewTitleUpdatePackageAvailableTitle(result.PackageInformation?.Version),
                        RuntimeEnvironment.IsUnpackaged ? SH.ServiceUpdateRecommendPackaged : SH.ViewTitileUpdatePackageAvailableContent,
                        SH.ViewTitleUpdatePackageSkipVersionButtonText)
                    .ConfigureAwait(false);

                if (installUpdateUserConsentResult is ContentDialogResult.Secondary)
                {
                    SkipVersion(result.PackageInformation?.Version, messenger);
                    return;
                }

                if (installUpdateUserConsentResult is not ContentDialogResult.Primary)
                {
                    return;
                }

                bool isUnpackaged = RuntimeEnvironment.IsUnpackaged;
                Uri? descriptorUri = isUnpackaged ? null : GetPackagedAppInstallerUri();
                if (!isUnpackaged && descriptorUri is null)
                {
                    messenger.Send(InfoBarMessage.Warning(SH.ServiceUpdateAppInstallerUnavailable));
                    return;
                }

                // Preview packages update from their own feeds. Mirror Chyan serves the public release.
                if (!isUnpackaged && !IsPublishPackage())
                {
                    await InstallFromFeedAsync(descriptorUri!).ConfigureAwait(false);
                    return;
                }

                DownloadSourceDialog sourceDialog = await contentDialogFactory
                    .CreateInstanceAsync<DownloadSourceDialog>(scope.ServiceProvider)
                    .ConfigureAwait(false);
                (bool confirmed, DownloadSourceKind source) = await sourceDialog.GetDownloadSourceAsync(offerPackaged: isUnpackaged).ConfigureAwait(false);
                if (!confirmed)
                {
                    return;
                }

                if (source is DownloadSourceKind.AppInstaller || (!isUnpackaged && source is DownloadSourceKind.Official))
                {
                    await InstallFromFeedAsync(descriptorUri ?? GetAppInstallerUri("Publish")).ConfigureAwait(false);
                    return;
                }

                if (source is DownloadSourceKind.MirrorChyan)
                {
                    MirrorChyanCdkDialog cdkDialog = await contentDialogFactory
                        .CreateInstanceAsync<MirrorChyanCdkDialog>(scope.ServiceProvider)
                        .ConfigureAwait(false);
                    (bool accepted, string cdk) = await cdkDialog.GetCdkAsync().ConfigureAwait(false);
                    if (!accepted)
                    {
                        return;
                    }

                    LocalSetting.Set(SettingKeys.MirrorChyanCdk, cdk);
                }
                else if (result.PackageInformation?.Validation is not { Length: > 0 })
                {
                    messenger.Send(InfoBarMessage.Error(SH.ViewModelSettingCheckUpdateFailed));
                    return;
                }

                await LaunchUpdaterAsync(source).ConfigureAwait(false);
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

    private static bool IsVersionSkipped(Version? version)
    {
        return version is not null && LocalSetting.Get(SettingKeys.SkippedUpdateVersion, string.Empty) == version.ToString();
    }

    private static void SkipVersion(Version? version, IMessenger messenger)
    {
        if (version is null)
        {
            return;
        }

        LocalSetting.Set(SettingKeys.SkippedUpdateVersion, version.ToString());
        messenger.Send(InfoBarMessage.Success(SH.FormatViewTitleUpdatePackageSkipVersionInfoBarMessage(version)));
    }

    private static async ValueTask<Version?> GetRemoteAppInstallerVersionAsync(Uri uri, CancellationToken token)
    {
        using HttpClient client = new();
        using HttpResponseMessage response = await client.GetAsync(uri, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using Stream stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        XDocument descriptor = await XDocument.LoadAsync(stream, LoadOptions.None, token).ConfigureAwait(false);
        XElement? root = descriptor.Root;
        // Duplicate MainPackage elements are also a malformed remote descriptor.
        XElement[] mainPackages = root?.Elements().Where(element => element.Name.LocalName == "MainPackage").Take(2).ToArray() ?? [];
        XElement? main = mainPackages.Length == 1 ? mainPackages[0] : null;
        if (root is null
            || root.Name.LocalName != "AppInstaller"
            || (string?)root.Attribute("Uri") != uri.AbsoluteUri
            || (string?)main?.Attribute("Name") != Package.Current.Id.Name
            || (string?)main?.Attribute("Publisher") != Package.Current.Id.Publisher)
        {
            return null;
        }

        string? value = (string?)main?.Attribute("Version");
        return Version.TryParse(value, out Version? remoteVersion) ? remoteVersion : null;
    }

    private static bool IsPublishPackage() => string.Equals(Package.Current.Id.Name, PublishPackageName, StringComparison.Ordinal);

    private static Uri GetAppInstallerUri(string channel) => new($"https://{AppInstallerHost}/{channel}/Snap.Hutao.Remastered.{channel}.x64.appinstaller");

    private static Uri? GetPackagedAppInstallerUri()
    {
        return Package.Current.Id.Name switch
        {
            PublishPackageName => GetAppInstallerUri("Publish"),
            "7f0db578-026f-4e0b-a75b-d5d06bb0a74c" => GetAppInstallerUri("Alpha"),
            "52127695-c6a7-406e-916a-693b905e8ba7" => GetAppInstallerUri("Canary"),
            _ => null,
        };
    }

    private static async ValueTask InstallFromFeedAsync(Uri descriptorUri)
    {
        // Use the Windows deployment API with the hosted XML. This does not require
        // a local descriptor or the browser's ms-appinstaller protocol handler.
        PackageManager packageManager = new();
        await packageManager.RequestAddPackageByAppInstallerFileAsync(
            descriptorUri,
            AddPackageByAppInstallerOptions.ForceTargetAppShutdown,
            packageManager.GetDefaultPackageVolume());
    }

    private async ValueTask LaunchUpdaterAsync(DownloadSourceKind downloadSource = DownloadSourceKind.Official)
    {
        using SentrySpanScope span = SentrySpanScope.StartChild("update.launch_updater", $"{downloadSource}");
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
