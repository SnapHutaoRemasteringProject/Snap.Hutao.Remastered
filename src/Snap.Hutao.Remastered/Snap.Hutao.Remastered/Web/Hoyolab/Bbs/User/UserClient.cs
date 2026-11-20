// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Core.IO.Http;
using Snap.Hutao.Remastered.Web.Endpoint.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.DataSigning;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using Snap.Hutao.Remastered.Web.Response;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Bbs.User;

[HttpClient(HttpClientConfiguration.XRpc)]
public sealed partial class UserClient : IUserClient
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    [FromKeyed(ApiEndpointsKind.Chinese)]
    private readonly IApiEndpoints apiEndpoints;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial UserClient(IServiceProvider serviceProvider, HttpClient httpClient);

    public async ValueTask<Response<UserFullInfoWrapper>> GetUserFullInfoAsync(Model.Entity.User user, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(user.Aid);

        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(apiEndpoints.CurrentUserFullInfo(user.Aid))
            .SetUserCookieAndFpHeader(user, CookieType.SToken)
            .SetHeader("User-Agent", HoyolabOptions.MobileUserAgent)
            .SetHeader("x-rpc-client_type", "2")
            .SetReferer(apiEndpoints.BbsReferer())
            .SetOptions(RetryHttpHandler.DisableRetry, true)
            .Get();

        await builder.SignDataAsync(DataSignAlgorithmVersion.Gen1, SaltType.K2, true).ConfigureAwait(false);
        Response<UserFullInfoWrapper>? resp = await builder
            .SendAsync<Response<UserFullInfoWrapper>>(httpClient, token)
            .ConfigureAwait(false);

        return Response.Response.DefaultIfNull(resp);
    }
}
