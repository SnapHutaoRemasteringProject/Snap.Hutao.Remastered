// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using Windows.Management.Deployment;

namespace Snap.Hutao.Remastered.Service.Update;

public sealed partial class UpdateService
{
    private static async ValueTask InstallMirrorMsixAsync(string cdk, CancellationToken token)
    {
        Uri packageUri = await GetMirrorMsixUriAsync(cdk, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();

        // Give Windows the remote MSIX so it can reuse unchanged package blocks.
        PackageManager packageManager = new();
        await packageManager.UpdatePackageAsync(packageUri, [], DeploymentOptions.ForceApplicationShutdown);
    }

    private static async ValueTask<Uri> GetMirrorMsixUriAsync(string cdk, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(cdk))
        {
            throw new InvalidDataException(SH.ServiceUpdateMirrorChyanPackageUnavailable);
        }

        // Match the platform of SnapHutaoRemastered_msix in mirrorchyan_release.yml.
        // Keep the CDK request outside the app's tracing handlers.
        using HttpClient client = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        string endpoint = $"https://mirrorchyan.com/api/resources/SnapHutaoRemastered_msix/latest?os=win&arch=x64&cdk={Uri.EscapeDataString(cdk.Trim())}";
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
        if (Uri.TryCreate(result?.Data?.Url, UriKind.Absolute, out Uri? packageUri)
            && packageUri.Scheme == Uri.UriSchemeHttps
            && packageUri.UserInfo.Length == 0)
        {
            return packageUri;
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
}
