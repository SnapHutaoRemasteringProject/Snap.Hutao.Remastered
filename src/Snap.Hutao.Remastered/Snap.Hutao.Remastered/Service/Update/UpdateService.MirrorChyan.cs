// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Logging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using Windows.Management.Deployment;

namespace Snap.Hutao.Remastered.Service.Update;

public sealed partial class UpdateService
{
    private static async Task InstallMirrorMsixAsync(string cdk, bool isAlpha, Version? offeredVersion, IProgress<DeploymentProgress> progress, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(cdk))
        {
            throw new InvalidDataException(SH.ServiceUpdateMirrorChyanPackageUnavailable);
        }

        MirrorChyanMsixPackage package = await GetMirrorMsixPackageAsync(cdk.Trim(), isAlpha, token).ConfigureAwait(false);
        if ((isAlpha && (package.Version is not { } version || version <= HutaoRuntime.Version || (offeredVersion is not null && version < offeredVersion)))
            || !Uri.TryCreate(package.Url, UriKind.Absolute, out Uri? packageUri)
            || packageUri.Scheme != Uri.UriSchemeHttps
            || packageUri.UserInfo.Length != 0)
        {
            throw new InvalidDataException(SH.ServiceUpdateMirrorChyanPackageUnavailable);
        }

        token.ThrowIfCancellationRequested();

        // Give Windows the remote MSIX so it can reuse unchanged package blocks.
        PackageManager packageManager = new();
        await packageManager.UpdatePackageAsync(packageUri, [], DeploymentOptions.ForceApplicationShutdown).AsTask(progress).ConfigureAwait(false);
    }

    private static async ValueTask<Version?> TryGetMirrorAlphaVersionAsync(CancellationToken token)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            MirrorChyanMsixPackage package = await GetMirrorMsixPackageAsync(null, isAlpha: true, timeout.Token).ConfigureAwait(false);
            return package.Version;
        }
        catch (Exception exception) when ((exception is HttpRequestException or IOException or JsonException or OperationCanceledException)
            && !token.IsCancellationRequested)
        {
            SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateDebug("Mirror Chyan Alpha version lookup failed", "update"));
            return null;
        }
    }

    private static async ValueTask<MirrorChyanMsixPackage> GetMirrorMsixPackageAsync(string? cdk, bool isAlpha, CancellationToken token)
    {
        // Match the platform of SnapHutaoRemastered_msix in mirrorchyan_release.yml.
        // Keep the CDK request outside the app's tracing handlers.
        using HttpClient client = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        string channel = isAlpha ? "alpha" : "stable";
        string endpoint = $"https://mirrorchyan.com/api/resources/SnapHutaoRemastered_msix/latest?os=win&arch=x64&channel={channel}";
        if (cdk is not null)
        {
            endpoint += $"&cdk={Uri.EscapeDataString(cdk)}";
        }

        using HttpResponseMessage response = await client.GetAsync(endpoint, token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType != "application/json")
        {
            response.EnsureSuccessStatusCode();
        }

        MirrorChyanMsixResponse? result = await response.Content.ReadFromJsonAsync(MirrorChyanMsixJsonContext.Default.MirrorChyanMsixResponse, token).ConfigureAwait(false);
        if (result is { Code: not 0 })
        {
            throw new InvalidDataException(SH.FormatServiceUpdateMirrorChyanApiError(result.Code));
        }

        response.EnsureSuccessStatusCode();
        // The Alpha lookup can return a stable release if no matching Alpha exists.
        if (result?.Data is { } package && (!isAlpha || string.Equals(package.Channel, channel, StringComparison.OrdinalIgnoreCase)))
        {
            return package;
        }

        throw new InvalidDataException(SH.ServiceUpdateMirrorChyanPackageUnavailable);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(MirrorChyanMsixResponse))]
internal sealed partial class MirrorChyanMsixJsonContext : JsonSerializerContext;

internal sealed class MirrorChyanMsixResponse
{
    public required int Code { get; init; }

    public MirrorChyanMsixPackage? Data { get; init; }
}

internal sealed class MirrorChyanMsixPackage
{
    public string? Url { get; init; }

    public string? Channel { get; init; }

    [JsonPropertyName("version_name")]
    public string? VersionName { get; init; }

    [JsonIgnore]
    public Version? Version => System.Version.TryParse(VersionName?.TrimStart('v', 'V'), out Version? version) ? version : null;
}
