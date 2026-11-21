// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.DependencyInjection.Annotation.HttpClient;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Web.Endpoint.Hoyolab;
using Snap.Hutao.Remastered.Web.Hoyolab.DataSigning;
using Snap.Hutao.Remastered.Web.Hutao;
using Snap.Hutao.Remastered.Web.Request.Builder;
using Snap.Hutao.Remastered.Web.Request.Builder.Abstraction;
using Snap.Hutao.Remastered.Web.Response;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Passport;

[HttpClient(HttpClientConfiguration.XRpcPassport)]
[PrimaryHttpMessageHandler(UseCookies = false)]
public sealed partial class PassportClient : IPassportClient
{
    private const string AndroidVersion = "12";
    private static readonly string LifecycleId = Guid.NewGuid().ToString();
    private static readonly InfrastructureJsonContext PassportJson = new(new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });

    private readonly IHttpRequestMessageBuilderFactory httpRequestMessageBuilderFactory;
    [FromKeyed(ApiEndpointsKind.Chinese)]
    private readonly IApiEndpoints apiEndpoints;
    private readonly HttpClient httpClient;

    [GeneratedConstructor]
    public partial PassportClient(IServiceProvider serviceProvider, HttpClient httpClient);

    public async ValueTask<Response<UidCookieToken>> GetCookieAccountInfoBySTokenAsync(User user, CancellationToken token = default)
    {
        Response<PassportTokenExchange.Result> response = await ExchangeTokenAsync(user, PassportTokenExchange.TokenKind.CookieToken, token).ConfigureAwait(false);
        UidCookieToken? credential = response.Data?.Token?.Value is { } value
            ? new() { Uid = user.Aid!, CookieToken = value }
            : null;
        return new(response.ReturnCode, response.Message, credential);
    }

    public async ValueTask<Response<LTokenWrapper>> GetLTokenBySTokenAsync(User user, CancellationToken token = default)
    {
        Response<PassportTokenExchange.Result> response = await ExchangeTokenAsync(user, PassportTokenExchange.TokenKind.LToken, token).ConfigureAwait(false);
        LTokenWrapper? credential = response.Data?.Token?.Value is { } value ? new() { LToken = value } : null;
        return new(response.ReturnCode, response.Message, credential);
    }

    public async ValueTask<Response<UserInfoWrapper>> VerifyLtokenAsync(User user, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(apiEndpoints.AccountVerifyLtoken())
            .SetUserCookieAndFpHeader(user, CookieType.LToken)
            .PostJson(new Dictionary<string, string> { ["ltoken"] = user.LToken?.GetValueOrDefault(Cookie.LTOKEN_V2) ?? string.Empty });
        await builder.SignDataAsync(DataSignAlgorithmVersion.Gen2, SaltType.X4, false).ConfigureAwait(false);

        Response<UserInfoWrapper>? resp = await builder
            .SendAsync<Response<UserInfoWrapper>>(httpClient, token)
            .ConfigureAwait(false);

        return Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<Response<LoginResult>> LoginBySTokenAsync(Cookie stokenV1, CancellationToken token = default)
    {
        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(apiEndpoints.AccountGetSTokenByOldToken())
            .SetHeader("Cookie", stokenV1.ToString())
            .Post();

        await builder.SignDataAsync(DataSignAlgorithmVersion.Gen2, SaltType.PROD, true).ConfigureAwait(false);

        Response<LoginResult>? resp = await builder
            .SendAsync<Response<LoginResult>>(httpClient, token)
            .ConfigureAwait(false);

        return Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<Response<LoginResult>> LoginByGameTokenAsync(UidGameToken account, CancellationToken token = default)
    {
        AccountIdGameToken data = new()
        {
            AccountId = int.Parse(account.Uid, CultureInfo.InvariantCulture),
            GameToken = account.GameToken,
        };

        HttpRequestMessageBuilder builder = httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(apiEndpoints.AccountGetSTokenByGameToken())
            .PostJson(data);

        Response<LoginResult>? resp = await builder
            .SendAsync<Response<LoginResult>>(httpClient, token)
            .ConfigureAwait(false);

        return Response.Response.DefaultIfNull(resp);
    }

    public async ValueTask<(string? Aigis, Response<MobileCaptcha> Response)> CreateLoginCaptchaAsync(string mobile, string? aigis, CancellationToken token = default)
    {
        Dictionary<string, string> data = new()
        {
            ["area_code"] = Encrypt("+86"),
            ["mobile"] = Encrypt(mobile),
        };

        HttpRequestMessageBuilder builder = CreatePassportRequest(apiEndpoints.AccountCreateLoginCaptcha(), data, PassportJson.StringDictionary)
            .SetReferer("https://user.miyoushe.com/");

        if (!string.IsNullOrEmpty(aigis))
        {
            builder.SetXrpcAigis(aigis);
        }

        await builder.SignDataAsync(DataSignAlgorithmVersion.Gen2, SaltType.Passport, true).ConfigureAwait(false);

        (HttpResponseHeaders? headers, Response<MobileCaptcha>? resp) = await builder
            .SendAsync<Response<MobileCaptcha>>(httpClient, token)
            .ConfigureAwait(false);

        string? rpcAigis = headers?.GetValuesOrDefault("X-Rpc-Aigis")?.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
        return (rpcAigis, Response.Response.DefaultIfNull(resp));
    }

    public ValueTask<Response<LoginResult>> LoginByMobileCaptchaAsync(IPassportMobileCaptchaProvider provider, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(provider.ActionType);
        ArgumentNullException.ThrowIfNull(provider.Mobile);
        ArgumentNullException.ThrowIfNull(provider.Captcha);

        return LoginByMobileCaptchaAsync(provider.ActionType, provider.Mobile, provider.Captcha, provider.Aigis, token);
    }

    public async ValueTask<Response<LoginResult>> LoginByMobileCaptchaAsync(string actionType, string mobile, string captcha, string? aigis, CancellationToken token = default)
    {
        Dictionary<string, string> data = new()
        {
            ["area_code"] = Encrypt("+86"),
            ["action_type"] = actionType,
            ["captcha"] = captcha,
            ["mobile"] = Encrypt(mobile),
        };

        HttpRequestMessageBuilder builder = CreatePassportRequest(apiEndpoints.AccountLoginByMobileCaptcha(), data, PassportJson.StringDictionary);

        if (!string.IsNullOrEmpty(aigis))
        {
            builder.SetXrpcAigis(aigis);
        }

        await builder.SignDataAsync(DataSignAlgorithmVersion.Gen2, SaltType.Passport, true).ConfigureAwait(false);

        Response<LoginResult>? resp = await builder
            .SendAsync<Response<LoginResult>>(httpClient, token)
            .ConfigureAwait(false);

        return Response.Response.DefaultIfNull(resp);
    }

    public ValueTask<Response<ActionTicketInfo>> GetActionTicketInfoAsync(string ticket, CancellationToken token = default)
    {
        return ValueTask.FromException<Response<ActionTicketInfo>>(new NotSupportedException());
    }

    public ValueTask<Response.Response> VerifyActionTicketPartlyAsync(string ticket, string captcha, CancellationToken token = default)
    {
        return ValueTask.FromException<Response.Response>(new NotSupportedException());
    }

    public ValueTask<(string? Aigis, Response.Response Response)> CreateEmailCaptchaByActionTicketAsync(string ticket, string? aigis, CancellationToken token = default)
    {
        return ValueTask.FromException<(string? Aigis, Response.Response Response)>(new NotSupportedException());
    }

    private async ValueTask<Response<PassportTokenExchange.Result>> ExchangeTokenAsync(User user, PassportTokenExchange.TokenKind destination, CancellationToken token)
    {
        string? sToken = user.SToken?.GetValueOrDefault(Cookie.STOKEN);
        if (string.IsNullOrWhiteSpace(sToken) || string.IsNullOrWhiteSpace(user.Mid) || string.IsNullOrWhiteSpace(user.Aid))
        {
            return new(Response.Response.publicFailure, SH.ServiceUserProcessCookieRequestUserInfoFailed, null);
        }

        PassportTokenExchange.Request data = new(destination, new(sToken, PassportTokenExchange.TokenKind.SToken), user.Mid);
        HttpRequestMessageBuilder builder = CreatePassportRequest(apiEndpoints.AccountExchangeToken(), data, PassportJson.PassportExchangeRequest);
        if (!string.IsNullOrEmpty(user.Fingerprint))
        {
            builder.SetHeader("x-rpc-device_fp", Uri.EscapeDataString(user.Fingerprint));
        }

        await builder.SignDataAsync(DataSignAlgorithmVersion.Gen2, SaltType.Passport, true).ConfigureAwait(false);
        Response<PassportTokenExchange.Result>? result = await builder.SendAsync<Response<PassportTokenExchange.Result>>(httpClient, token).ConfigureAwait(false);
        Response<PassportTokenExchange.Result> response = Response.Response.DefaultIfNull(result);
        if (response.ReturnCode is not 0)
        {
            return response;
        }

        if (response.Data?.Matches(destination, user.Aid) is not true)
        {
            return new(Response.Response.publicFailure, SH.ServiceUserProcessCookieRequestUserInfoFailed, null);
        }

        return response;
    }

    private HttpRequestMessageBuilder CreatePassportRequest<T>(string endpoint, T data, JsonTypeInfo<T> typeInfo)
    {
        return httpRequestMessageBuilderFactory.Create()
            .SetRequestUri(endpoint)
            .SetHeader("User-Agent", $"Mozilla/5.0 (Linux; Android {AndroidVersion}) Mobile miHoYoBBS/{SaltConstants.CNVersion}")
            .SetHeader("x-rpc-sys_version", AndroidVersion)
            .SetHeader("x-rpc-lifecycle_id", LifecycleId)
            .SetStringContent(PassportTokenExchange.SerializeBody(data, typeInfo), Encoding.UTF8, MediaTypeNames.Application.Json)
            .Post();
    }

    private static string Encrypt(string source)
    {
        using (RSA rsa = RSA.Create())
        {
            // https://www.rfc-editor.org/rfc/rfc7468#section-3
            // https://github.com/UIGF-org/Hoyolab.Salt#cn-passport-api-rsa_public_key
            rsa.ImportFromPem("""
                -----BEGIN PUBLIC KEY-----
                MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDDvekdPMHN3AYhm/vktJT+YJr7
                cI5DcsNKqdsx5DZX0gDuWFuIjzdwButrIYPNmRJ1G8ybDIF7oDW2eEpm5sMbL9zs
                9ExXCdvqrn51qELbqj0XxtMTIpaCHFSI50PfPpTFV9Xt/hmyVwokoOXFlAEgCn+Q
                CgGs52bFoYMtyi+xEQIDAQAB
                -----END PUBLIC KEY-----
                """);
            return Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(source), RSAEncryptionPadding.Pkcs1));
        }
    }
}
