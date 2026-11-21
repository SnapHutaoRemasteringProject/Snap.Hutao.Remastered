// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Web.Endpoint.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.DataSigning;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using Snap.Hutao.Remastered.Web.Response;
using System.Collections.Immutable;
using System.Net.Http;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Binding;

[HttpClient(HttpClientConfiguration.XRpc)]
[PrimaryHttpMessageHandler(UseCookies = false)]
public sealed partial class BindingClient2
{
    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    [FromKeyed(ApiEndpointsKind.Chinese)]
    private readonly IApiEndpoints apiEndpoints;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial BindingClient2(IServiceProvider serviceProvider, HttpClient httpClient);

    public async ValueTask<Response<ListWrapper<UserGameRole>>> GetUserGameRolesBySTokenAsync(User user, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(apiEndpoints.UserGameRolesBySToken())
            .SetUserCookieAndFpHeader(user, CookieType.SToken)
            .SetHeader("x-rpc-client_type", "2")
            .SetReferer(apiEndpoints.AppReferer())
            .Get();

        await builder.SignDataAsync(DataSignAlgorithmVersion.Gen1, SaltType.K2, true).ConfigureAwait(false);

        Response<JsonElement>? response = await builder
            .SendAsync<Response<JsonElement>>(httpClient, token)
            .ConfigureAwait(false);

        if (response is null)
        {
            return Response.Response.DefaultIfNull<Response<ListWrapper<UserGameRole>>>(null);
        }

        if (response.ReturnCode is not 0)
        {
            return Response.Response.DefaultIfNull(Response.Response.CloneReturnCodeAndMessage<ListWrapper<UserGameRole>, JsonElement>(response));
        }

        if (response.Data.ValueKind != JsonValueKind.Object
            || !response.Data.TryGetProperty("list", out JsonElement list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return new(Response.Response.publicFailure, SH.ServiceUserGameRolesInvalidResponse, null);
        }

        ImmutableArray<UserGameRole>.Builder roles = ImmutableArray.CreateBuilder<UserGameRole>();
        bool rejectedGenshinRole = false;
        foreach (JsonElement item in list.EnumerateArray())
        {
            // This endpoint includes other games with regions outside RegionConverter's scope.
            if (item.ValueKind != JsonValueKind.Object
                || !item.TryGetProperty("game_biz", out JsonElement gameBiz)
                || gameBiz.ValueKind != JsonValueKind.String
                || !string.Equals(gameBiz.GetString(), "hk4e_cn", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!item.TryGetProperty("region", out JsonElement region) || region.ValueKind != JsonValueKind.String
                || !HoyolabRegex.RegionRegex.IsMatch(region.GetString()!))
            {
                rejectedGenshinRole = true;
                continue;
            }

            try
            {
                if (JsonSerialize.Deserialize<UserGameRole>(item, JsonOptions.Default) is { } role
                    && !string.IsNullOrWhiteSpace(role.GameUid))
                {
                    roles.Add(role);
                }
                else
                {
                    rejectedGenshinRole = true;
                }
            }
            catch (JsonException)
            {
                rejectedGenshinRole = true;
            }
        }

        return roles.Count is 0 && rejectedGenshinRole
            ? new(Response.Response.publicFailure, SH.ServiceUserGameRolesInvalidResponse, null)
            : new(0, response.Message, new() { List = roles.ToImmutable() });
    }

    public async ValueTask<Response<GameAuthKey>> GenerateAuthenticationKeyAsync(User user, GenAuthKeyData data, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(apiEndpoints.BindingGenAuthKey())
            .SetUserCookieAndFpHeader(user, CookieType.SToken)
            .SetReferer(apiEndpoints.AppReferer())
            .PostJson(data);

        await builder.SignDataAsync(DataSignAlgorithmVersion.Gen1, SaltType.LK2, true).ConfigureAwait(false);

        Response<GameAuthKey>? resp = await builder
            .SendAsync<Response<GameAuthKey>>(httpClient, token)
            .ConfigureAwait(false);

        return Response.Response.DefaultIfNull(resp);
    }
}
