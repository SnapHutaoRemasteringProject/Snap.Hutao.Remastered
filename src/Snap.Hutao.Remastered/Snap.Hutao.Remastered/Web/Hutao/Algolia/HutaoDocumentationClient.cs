// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Hutao.Algolia;

[HttpClient(HttpClientConfiguration.Default)]
public sealed partial class HutaoDocumentationClient
{
    private const string AlgoliaApiKey = "36f3346b302103834b15cfbb221ee810";
    private const string AlgoliaApplicationId = "626H2LTAQH";
    private const string AlgolianetIndexesQueries = $"https://626H2LTAQH-2.algolianet.com/1/indexes/*/queries";

    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial HutaoDocumentationClient(IServiceProvider serviceProvider, HttpClient httpClient);

    public async ValueTask<AlgoliaResponse?> QueryAsync(string query, string language, CancellationToken token = default)
    {
        AlgoliaRequestsWrapper data = new()
        {
            Requests =
            [
                new AlgoliaRequest
                {
                    Query = query,
                    IndexName = "hutao",
                    Params = $"""facetFilters=["lang:{language}"]""",
                },
            ],
        };

        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(AlgolianetIndexesQueries)
            .SetHeader("x-algolia-api-key", AlgoliaApiKey)
            .SetHeader("x-algolia-application-id", AlgoliaApplicationId)
            .PostJson(data);

        return await builder.SendAsync<AlgoliaResponse>(httpClient, token).ConfigureAwait(false);
    }
}