// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Hutao.Redeem;

[HttpClient(HttpClientConfiguration.Default)]
internal sealed partial class HutaoRedeemCodeClient
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly IHutaoEndpointsFactory hutaoEndpointsFactory;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial HutaoRedeemCodeClient(IServiceProvider serviceProvider, HttpClient httpClient);

    public async ValueTask<HutaoResponse<RedeemUseResult>> UseRedeemCodeAsync(string? accessToken, RedeemUseRequest request, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().RedeemCodeUse())
            .SetAccessToken(accessToken)
            .PostJson(request);

        HutaoResponse<RedeemUseResult>? resp = await builder
            .SendAsync<HutaoResponse<RedeemUseResult>>(httpClient, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }
}