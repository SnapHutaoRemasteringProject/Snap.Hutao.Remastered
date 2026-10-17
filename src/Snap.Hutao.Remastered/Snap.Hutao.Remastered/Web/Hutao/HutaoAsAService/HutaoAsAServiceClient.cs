// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Core.IO.Http;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Web.Endpoint.Hutao;
using Snap.Hutao.Remastered.Web.Hutao.Redeem;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Hutao.HutaoAsAService;

[HttpClient(HttpClientConfiguration.Default)]
public sealed partial class HutaoAsAServiceClient
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    private readonly IHutaoEndpointsFactory hutaoEndpointsFactory;
    private readonly CultureOptions cultureOptions;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial HutaoAsAServiceClient(IServiceProvider serviceProvider, HttpClient httpClient);

    public async ValueTask<HutaoResponse<ImmutableArray<Announcement>>> GetAnnouncementListAsync(ImmutableArray<long> excludedIds, CancellationToken token = default)
    {
        // This POST only reads announcements; excludedIds are a read filter.
        // Bound each attempt and retry once here, without enabling retries for
        // uploads/sign-ins or nesting another retry policy in RetryHttpHandler.
        for (int attempt = 0; attempt < 2; attempt++)
        {
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(token, XamlApplicationLifetime.ShutdownToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(8));
            deadline.Token.ThrowIfCancellationRequested();
            HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
                .SetRequestUri(hutaoEndpointsFactory.Create().Announcement(cultureOptions.LocaleName))
                .PostJson(excludedIds)
                .SetOptions(RetryHttpHandler.DisableRetry, true);

            try
            {
                using HttpRequestMessage request = builder.HttpRequestMessage;
                using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                // Read with the deadline directly. The generic serializer wraps
                // cancellation and rereads failed bodies without that deadline.
                string json = await response.Content.ReadAsStringAsync(deadline.Token).ConfigureAwait(false);
                JsonSerializerOptions? options = (builder.HttpContentSerializer as JsonHttpContentSerializer)?.JsonSerializerOptions ?? JsonOptions.Default;
                HutaoResponse<ImmutableArray<Announcement>>? body = string.IsNullOrEmpty(json)
                    ? null : JsonSerializer.Deserialize<HutaoResponse<ImmutableArray<Announcement>>>(json, options);
                return Web.Response.Response.DefaultIfNull(body);
            }
            catch (OperationCanceledException) when (attempt is 0 && !token.IsCancellationRequested && !XamlApplicationLifetime.ShutdownToken.IsCancellationRequested)
            {
                // A request deadline is retryable; caller/exit cancellation is not.
            }
            catch (HttpRequestException exception) when (attempt is 0
                && (!exception.StatusCode.HasValue || exception.StatusCode is HttpStatusCode.RequestTimeout || (int)exception.StatusCode.Value >= 500))
            {
            }
            catch (IOException) when (attempt is 0)
            {
            }
        }

        throw new InvalidOperationException("The announcement retry loop completed unexpectedly.");
    }

    public async ValueTask<HutaoResponse> UploadAnnouncementAsync(string? accessToken, UploadAnnouncement uploadAnnouncement, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().AnnouncementUpload())
            .SetAccessToken(accessToken)
            .PostJson(uploadAnnouncement);

        HutaoResponse? resp = await builder
            .SendAsync<HutaoResponse>(httpClient, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse> GachaLogCompensationAsync(string? accessToken, int days, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().GachaLogCompensation(days))
            .SetAccessToken(accessToken)
            .Get();

        HutaoResponse? resp = await builder
            .SendAsync<HutaoResponse>(httpClient, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse> GachaLogDesignationAsync(string? accessToken, string userName, int days, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().GachaLogDesignation(userName, days))
            .SetAccessToken(accessToken)
            .Get();

        HutaoResponse? resp = await builder
            .SendAsync<HutaoResponse>(httpClient, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse> CdnCompensationAsync(string? accessToken, int days, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().CdnCompensation(days))
            .SetAccessToken(accessToken)
            .Get();

        HutaoResponse? resp = await builder
            .SendAsync<HutaoResponse>(httpClient, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse> CdnDesignationAsync(string? accessToken, string userName, int days, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().CdnDesignation(userName, days))
            .SetAccessToken(accessToken)
            .Get();

        HutaoResponse? resp = await builder
            .SendAsync<HutaoResponse>(httpClient, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<HutaoResponse<RedeemGenerateResult>> GenerateRedeemCodesAsync(string? accessToken, RedeemGenerateRequest request, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(hutaoEndpointsFactory.Create().RedeemCodeGenerate())
            .SetAccessToken(accessToken)
            .PostJson(request);

        HutaoResponse<RedeemGenerateResult>? resp = await builder
            .SendAsync<HutaoResponse<RedeemGenerateResult>>(httpClient, token)
            .ConfigureAwait(false);

        return Web.Response.Response.DefaultIfNull(resp);
    }
}
