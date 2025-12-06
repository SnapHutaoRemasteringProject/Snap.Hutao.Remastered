// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Web.Endpoint.Hoyolab;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using Snap.Hutao.Remastered.Web.Response;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Hoyolab.PublicData.DeviceFp;

[HttpClient(HttpClientConfiguration.Default)]
internal sealed partial class DeviceFpClient
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    [FromKeyed(ApiEndpointsKind.Chinese)]
    private readonly IApiEndpoints apiEndpoints;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial DeviceFpClient(IServiceProvider serviceProvider, HttpClient httpClient);

    public async ValueTask<Response<DeviceFpWrapper>> GetFingerprintAsync(DeviceFpData data, CancellationToken token)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(apiEndpoints.DeviceFpGetFp())
            .PostJson(data);

        Response<DeviceFpWrapper>? resp = await builder
            .SendAsync<Response<DeviceFpWrapper>>(httpClient, token)
            .ConfigureAwait(false);

        return Response.Response.DefaultIfNull(resp);
    }
}