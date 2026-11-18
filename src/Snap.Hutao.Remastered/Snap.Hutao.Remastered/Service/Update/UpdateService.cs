// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Factory.ContentDialog;
using Snap.Hutao.Remastered.Service.Notification;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using Snap.Hutao.Remastered.Web.Response;
using System.IO;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Snap.Hutao.Remastered.Service.Update;

[Service(ServiceLifetime.Singleton, typeof(IUpdateService))]
public sealed partial class UpdateService : IUpdateService
{
    private const string AppInstallerFilename = "Snap.Hutao.Remastered_x64.appinstaller";
    private const string AppInstallerHost = "package.snaphutaorp.org";
    private const string StorePublisher = "CN=469D671A-3AED-4488-AC4F-75E5394BC7FA";
    private const string StoreProductId = "9P1LDJFJFJQQ";

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
                    _ => default,
                };
            }
        }
    }

    public async ValueTask TriggerUpdateAsync(CheckUpdateResult result, bool isAutomatic = false, CancellationToken token = default)
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
                        IsStorePackage ? SH.ServiceUpdateOpenStoreContent : SH.ViewTitileUpdatePackageAvailableContent,
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

                if (RuntimeEnvironment.IsUnpackaged)
                {
                    messenger.Send(InfoBarMessage.Error(SH.ServiceUpdateUnpackagedUnsupported));
                    return;
                }

                if (IsStorePackage)
                {
                    await OpenStoreAsync().ConfigureAwait(false);
                    return;
                }

                Uri? descriptorUri = GetPackagedAppInstallerUri();
                if (descriptorUri is null)
                {
                    messenger.Send(InfoBarMessage.Error(SH.ServiceUpdateAppInstallerUnavailable));
                    return;
                }

                // Read the stable HTTPS Uri from our signed, embedded descriptor, but
                // deploy using the current remote file (the embedded version is stale).
                // ms-appinstaller: has been disabled by Windows on consumer devices.
                PackageManager packageManager = new();
                await packageManager.RequestAddPackageByAppInstallerFileAsync(
                    descriptorUri,
                    AddPackageByAppInstallerOptions.ForceTargetAppShutdown,
                    packageManager.GetDefaultPackageVolume());
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

    public async ValueTask OpenStoreAsync()
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        IMessenger messenger = scope.ServiceProvider.GetRequiredService<IMessenger>();
        try
        {
            ITaskContext taskContext = scope.ServiceProvider.GetRequiredService<ITaskContext>();
            await taskContext.SwitchToMainThreadAsync();
            bool launched = await Windows.System.Launcher.LaunchUriAsync(new Uri($"ms-windows-store://pdp/?ProductId={StoreProductId}"));
            if (!launched)
            {
                launched = await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://apps.microsoft.com/detail/{StoreProductId}"));
            }

            if (!launched)
            {
                messenger.Send(InfoBarMessage.Error(SH.ServiceUpdateOpenStoreFailed));
            }
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(SH.ServiceUpdateOpenStoreFailed, ex));
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

    private static bool IsStorePackage => RuntimeEnvironment.IsPackaged
        && string.Equals(Package.Current.Id.Publisher, StorePublisher, StringComparison.Ordinal);

    private static Uri? GetPackagedAppInstallerUri()
    {
        string path = InstalledLocation.GetAbsolutePath(AppInstallerFilename);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            XDocument descriptor = XDocument.Load(path);
            XElement? root = descriptor.Root;
            if (root is null || root.Name.LocalName != "AppInstaller")
            {
                return null;
            }

            XElement? main = root.Elements().SingleOrDefault(element => element.Name.LocalName == "MainPackage");
            if (main is null
                || (string?)main.Attribute("Name") != Package.Current.Id.Name
                || (string?)main.Attribute("Publisher") != Package.Current.Id.Publisher)
            {
                return null;
            }

            string? source = (string?)root.Attribute("Uri");
            return Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && string.Equals(uri.Host, AppInstallerHost, StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath == "/" + AppInstallerFilename
                ? uri
                : null;
        }
        catch (Exception)
        {
            // Store, legacy or malformed packages must not silently switch channel.
            return null;
        }
    }
}
