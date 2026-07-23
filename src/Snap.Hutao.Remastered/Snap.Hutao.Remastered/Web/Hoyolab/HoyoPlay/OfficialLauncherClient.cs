// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Web.Endpoint.Hoyolab;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Hoyolab.HoyoPlay;

[HttpClient(HttpClientConfiguration.Default)]
public sealed partial class OfficialLauncherClient
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly ILogger<OfficialLauncherClient> logger;
    private readonly IApiEndpoints apiEndpoints;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial OfficialLauncherClient(IServiceProvider serviceProvider, HttpClient httpClient);

    public async ValueTask<string?> GetBackgroundVideoUrlAsync(CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(apiEndpoints.HoyoPlayAllGameBasicInfo())
            .Get();

        OfficialLauncherBackground? resp = await builder.SendAsync<OfficialLauncherBackground>(httpClient, token).ConfigureAwait(false);

        if (resp?.Data?.GameInfoList is not { } gameInfoList)
        {
            return null;
        }

        foreach (OfficialLauncherGameInfo gameInfo in gameInfoList)
        {
            if (gameInfo.Game?.Biz is not "hk4e_cn")
            {
                continue;
            }

            if (gameInfo.Backgrounds is not { } backgrounds)
            {
                continue;
            }

            foreach (OfficialLauncherBackgroundItem item in backgrounds)
            {
                if (item.Type is "BACKGROUND_TYPE_VIDEO" && !string.IsNullOrEmpty(item.Video?.Url))
                {
                    return item.Video.Url;
                }
            }
        }

        return null;
    }
}
