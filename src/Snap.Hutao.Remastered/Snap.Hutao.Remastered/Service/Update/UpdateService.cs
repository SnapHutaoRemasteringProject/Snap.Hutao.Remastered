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
using System.Net.Http;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Snap.Hutao.Remastered.Service.Update;

[Service(ServiceLifetime.Singleton, typeof(IUpdateService))]
public sealed partial class UpdateService : IUpdateService
{
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

                // Self-hosted releases must compare against THEIR own channel.
                // The infrastructure version API represents Store/Publish, not Alpha/Canary
                // or the selected R2R/NativeAOT stream.
                if (RuntimeEnvironment.IsPackaged && !IsStorePackage)
                {
                    Uri? descriptorUri = GetPackagedAppInstallerUri();
                    if (descriptorUri is null)
                    {
                        checkUpdateResult.Kind = CheckUpdateResultKind.AppInstallerUnavailable;
                        return checkUpdateResult;
                    }

                    Version? remoteVersion;
                    try
                    {
                        remoteVersion = await GetRemoteAppInstallerVersionAsync(descriptorUri, token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is HttpRequestException or IOException or System.Xml.XmlException
                        || (ex is OperationCanceledException && !token.IsCancellationRequested))
                    {
                        // Offline, HTTP and malformed remote responses are normal check failures;
                        // the caller needs a result to display rather than an unhandled command exception.
                        SentrySdk.CaptureException(ex);
                        checkUpdateResult.Kind = CheckUpdateResultKind.VersionApiInvalidResponse;
                        return checkUpdateResult;
                    }

                    if (remoteVersion is null)
                    {
                        checkUpdateResult.Kind = CheckUpdateResultKind.VersionApiInvalidResponse;
                        return checkUpdateResult;
                    }

                    checkUpdateResult.PackageInformation = new HutaoPackageInformation { Version = remoteVersion };
                    checkUpdateResult.Kind = !LocalSetting.Get(SettingKeys.OverrideUpdateVersionComparison, false)
                        && HutaoRuntime.Version >= remoteVersion
                        ? CheckUpdateResultKind.AlreadyUpdated
                        : CheckUpdateResultKind.UpdateAvailable;
                    return checkUpdateResult;
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

    private static Uri? GetPackagedAppInstallerUri()
    {
        try
        {
            // Each signed package embeds exactly one descriptor identifying its
            // own channel and runtime. Never switch streams based on the API.
            string directory = InstalledLocation.GetAbsolutePath(string.Empty);
            string[] descriptors = Directory.GetFiles(directory, "Snap.Hutao.Remastered.*.x64.appinstaller");
            if (descriptors.Length != 1)
            {
                return null;
            }

            string filename = Path.GetFileName(descriptors[0]);
            XDocument descriptor = XDocument.Load(descriptors[0]);
            XElement? root = descriptor.Root;
            XElement? main = root?.Elements().SingleOrDefault(element => element.Name.LocalName == "MainPackage");
            if (root is null
                || root.Name.LocalName != "AppInstaller"
                || (string?)main?.Attribute("Name") != Package.Current.Id.Name
                || (string?)main?.Attribute("Publisher") != Package.Current.Id.Publisher)
            {
                return null;
            }

            string? source = (string?)root.Attribute("Uri");
            if (!Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !string.Equals(uri.Host, AppInstallerHost, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string[] parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // The default (R2R) feed lives directly beneath the channel,
            // while native AOT has a separate /AOT/ subdirectory and .AOT filename.
            if (parts.Length is not (2 or 3)
                || parts[0] is not ("Publish" or "Alpha" or "Canary")
                || (parts.Length == 3 && parts[1] != "AOT"))
            {
                return null;
            }

            string runtimeSuffix = parts.Length == 3 ? ".AOT" : string.Empty;
            string expectedFilename = $"Snap.Hutao.Remastered.{parts[0]}{runtimeSuffix}.x64.appinstaller";
            if (filename != expectedFilename || parts[^1] != expectedFilename)
            {
                return null;
            }

            return uri;
        }
        catch (Exception ex)
        {
            // Reject legacy, malformed or incompatible package descriptors, but
            // report file access and XML failures instead of silently swallowing them.
            SentrySdk.CaptureException(ex);
            return null;
        }
    }
}
