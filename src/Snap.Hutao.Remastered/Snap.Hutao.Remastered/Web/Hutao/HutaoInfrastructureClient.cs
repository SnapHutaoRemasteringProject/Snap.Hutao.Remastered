// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Core.IO.Http;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using Snap.Hutao.Remastered.Win32;
using System.Collections.Immutable;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Hutao;

[HttpClient(HttpClientConfiguration.Default)]
public sealed partial class HutaoInfrastructureClient
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly IHutaoEndpointsFactory hutaoEndpointsFactory;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial HutaoInfrastructureClient(IServiceProvider serviceProvider, HttpClient httpClient);

    public async ValueTask<HutaoResponse<StaticResourceSizeInformation>> GetStaticSizeAsync(CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(StaticResourcesEndpoints.StaticSize())
            .Get();

        HutaoResponse<StaticResourceSizeInformation>? resp = await builder.SendAsync<HutaoResponse<StaticResourceSizeInformation>>(httpClient, token).ConfigureAwait(false);
        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse<IPInformation>> GetIPInformationAsync(CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().Ip())
            .Get();

        HutaoResponse<IPInformation>? resp = await builder.SendAsync<HutaoResponse<IPInformation>>(httpClient, token).ConfigureAwait(false);
        return Web.Response.Response.DefaultIfNull(resp);
    }

    /// <summary>
    /// Lightweight connectivity probe against the Snap Hutao API. Used as a fallback when
    /// NCSI's cached connectivity state is unreliable. Returns true only when a valid JSON
    /// response is received, so hijacked/offline connections are reported as unreachable.
    /// </summary>
    public async ValueTask<bool> IsReachableAsync(CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().Ip())
            .SetOptions(RetryHttpHandler.DisableRetry, true)
            .Get();

        HutaoResponse<IPInformation>? resp = await builder.SendAsync<HutaoResponse<IPInformation>>(httpClient, token).ConfigureAwait(false);
        return resp is not null;
    }

    public async ValueTask<HutaoResponse<HutaoPackageInformation>> GetHutaoVersionInformationAsync(CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().PatchSnapHutao())
            .Get();

        HutaoResponse<HutaoPackageInformation>? resp = await builder.SendAsync<HutaoResponse<HutaoPackageInformation>>(httpClient, token).ConfigureAwait(false);
        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse> AmIBannedAsync(string uid, CancellationToken token)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().AmIBanned())
            .SetHeader("x-hutao-island-identifier", HutaoNative.Instance.ExchangeGameUidForIdentifier1820(uid))
            .Get();

        HutaoResponse? resp = await builder.SendAsync<HutaoResponse>(httpClient, token).ConfigureAwait(false);
        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse<ImmutableArray<GitRepository>>> GetGitRepositoryAsync(string name, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().GitRepository(name))
            .Get();

        HutaoResponse<ImmutableArray<GitRepository>>? resp = await builder.SendAsync<HutaoResponse<ImmutableArray<GitRepository>>>(httpClient, token).ConfigureAwait(false);
        return Web.Response.Response.DefaultIfNull(resp);
    }
}