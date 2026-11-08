// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core.LifeCycle;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.UI.Content;
using Snap.Hutao.Remastered.UI.Xaml.Behavior.Action;
using Snap.Hutao.Remastered.UI.Xaml.View.Window.WebView2;
using Snap.Hutao.Remastered.Web.Hoyolab.Passport;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Verification;
using Snap.Hutao.Remastered.Web.Hutao.Geetest;
using Snap.Hutao.Remastered.Web.Response;
using System.Text;

namespace Snap.Hutao.Remastered.Service.Geetest;

[Service(ServiceLifetime.Transient, typeof(IGeetestService))]
public sealed partial class GeetestService : IGeetestService
{
    private static readonly JsonSerializerOptions CompactJsonOptions = JsonOptions.CreateCompact();

    private readonly ICurrentXamlWindowReference currentXamlWindowReference;
    private readonly CustomGeetestClient customGeetestClient;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly ITaskContext taskContext;
    private readonly CardClient cardClient;
    private readonly IMessenger messenger;

    [GeneratedConstructor]
    public partial GeetestService(IServiceProvider serviceProvider);

    public async ValueTask<GeetestData?> TryVerifyGtChallengeAsync(string gt, string challenge, bool isOversea, CancellationToken token = default)
    {
        GeetestResponse response = await customGeetestClient.VerifyAsync(gt, challenge, token).ConfigureAwait(false);

        if (response is { Code: 0, Data: { } data })
        {
            return data;
        }

        string? result = await PrivateVerifyByWebViewAsync(() => new(gt, challenge, isOversea), token).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result))
        {
            return default;
        }

        GeetestWebResponse? webResponse = JsonSerialize.Deserialize<GeetestWebResponse>(result, jsonOptions);
        ArgumentNullException.ThrowIfNull(webResponse);

        return new()
        {
            Gt = gt,
            Challenge = webResponse.Challenge,
            Validate = webResponse.Validate,
        };
    }

    public async ValueTask<string?> TryVerifyXrpcChallengeAsync(Model.Entity.User user, CardVerificationHeaders headers, CancellationToken token = default)
    {
        Response<GeetestVerification> registrationResponse = await cardClient.CreateVerificationAsync(user, headers, token).ConfigureAwait(false);
        if (!ResponseValidator.TryValidate(registrationResponse, messenger, out GeetestVerification? registration))
        {
            return default;
        }

        if (await TryVerifyGtChallengeAsync(registration.Gt, registration.Challenge, user.IsOversea, token).ConfigureAwait(false) is not { } data)
        {
            return default;
        }

        Response<VerificationResult> verifyResponse = await cardClient.VerifyVerificationAsync(user, headers, data.Challenge, data.Validate, token).ConfigureAwait(false);
        if (!ResponseValidator.TryValidate(verifyResponse, messenger, out VerificationResult? result))
        {
            return default;
        }

        return result.Challenge;
    }

    public async ValueTask<bool> TryVerifyAigisSessionAsync(IAigisProvider provider, string? rawSession, bool isOversea, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(rawSession))
        {
            return false;
        }

        string sessionId;
        string? gt;
        string? challenge;
        string? captchaId;
        string? riskType;
        try
        {
            AigisSession? session = JsonSerialize.Deserialize<AigisSession>(rawSession, jsonOptions);
            if (session is null || string.IsNullOrWhiteSpace(session.SessionId))
            {
                System.Diagnostics.Debug.WriteLine("Passport Aigis: response has no verification session ID.");
                return false;
            }

            // Aigis can wrap the challenge as either a JSON object or a string containing JSON.
            using JsonDocument? nestedData = session.Data.ValueKind == JsonValueKind.String
                ? JsonDocument.Parse(session.Data.GetString()!)
                : null;
            JsonElement data = nestedData?.RootElement ?? session.Data;
            sessionId = session.SessionId;
            gt = GetChallengeField(data, "gt");
            challenge = GetChallengeField(data, "challenge");
            captchaId = GetChallengeField(data, "captcha_id");
            riskType = GetChallengeField(data, "risk_type");
        }
        catch (JsonException)
        {
            System.Diagnostics.Debug.WriteLine("Passport Aigis: challenge JSON could not be read.");
            return false;
        }

        // Some Passport V4 challenges supply the CAPTCHA identifier as gt and omit the V3 challenge string.
        if (string.IsNullOrWhiteSpace(captchaId) && string.IsNullOrWhiteSpace(challenge))
        {
            captchaId = gt;
        }

        Func<GeetestWebView2ContentProvider> createProvider;
        if (!string.IsNullOrWhiteSpace(captchaId))
        {
            createProvider = () => new(captchaId, sessionId, riskType);
        }
        else if (!string.IsNullOrWhiteSpace(gt) && !string.IsNullOrWhiteSpace(challenge))
        {
            createProvider = () => new(gt, challenge, isOversea);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Passport Aigis: unsupported CAPTCHA fields.");
            return false;
        }

        bool isV4 = !string.IsNullOrWhiteSpace(captchaId);
        System.Diagnostics.Debug.WriteLine($"Passport Aigis: opening GeeTest {(isV4 ? "V4" : "V3")}.");
        string? webResult = await PrivateVerifyByWebViewAsync(createProvider, token, provider as FrameworkElement).ConfigureAwait(false);
        if (string.IsNullOrEmpty(webResult))
        {
            return false;
        }

        string verification = webResult;
        try
        {
            if (isV4)
            {
                using JsonDocument document = JsonDocument.Parse(webResult);
                JsonElement data = document.RootElement;
                if (string.IsNullOrWhiteSpace(GetChallengeField(data, "lot_number"))
                    || string.IsNullOrWhiteSpace(GetChallengeField(data, "captcha_output"))
                    || string.IsNullOrWhiteSpace(GetChallengeField(data, "pass_token"))
                    || !data.TryGetProperty("gen_time", out JsonElement time)
                    || time.ValueKind is not (JsonValueKind.String or JsonValueKind.Number)
                    || string.IsNullOrWhiteSpace(time.ToString())
                    || (GetChallengeField(data, "captcha_id") is { } returnedId && !string.Equals(returnedId, captchaId, StringComparison.Ordinal)))
                {
                    return false;
                }
            }
            else
            {
                GeetestWebResponse? result = JsonSerialize.Deserialize<GeetestWebResponse>(webResult, jsonOptions);
                if (result is null || string.IsNullOrWhiteSpace(result.Challenge) || string.IsNullOrWhiteSpace(result.Validate))
                {
                    return false;
                }

                verification = JsonSerialize.Serialize(result, CompactJsonOptions);
            }
        }
        catch (JsonException)
        {
            return false;
        }

        provider.Aigis = $"{sessionId};{Convert.ToBase64String(Encoding.UTF8.GetBytes(verification))}";
        return true;
    }

    private static string? GetChallengeField(JsonElement data, string name)
    {
        return data.ValueKind == JsonValueKind.Object && data.TryGetProperty(name, out JsonElement field) && field.ValueKind == JsonValueKind.String
            ? field.GetString()
            : null;
    }

    private async ValueTask<string?> PrivateVerifyByWebViewAsync(Func<GeetestWebView2ContentProvider> createProvider, CancellationToken token, FrameworkElement? owner = null)
    {
        if (XamlApplicationLifetime.Exiting)
        {
            return default;
        }

        await taskContext.SwitchToMainThreadAsync();
        token.ThrowIfCancellationRequested();
        XamlRoot? ownerRoot = owner?.XamlRoot;
        XamlRoot? xamlRoot = ownerRoot?.XamlContext() is not null ? ownerRoot : currentXamlWindowReference.XamlRoot;
        if (xamlRoot?.XamlContext() is not null)
        {
            GeetestWebView2ContentProvider contentProvider = createProvider();
            ShowWebView2WindowAction.Show(contentProvider, xamlRoot);

            await taskContext.SwitchToBackgroundAsync();
            token.ThrowIfCancellationRequested();
            return await contentProvider.GetResultAsync().ConfigureAwait(false);
        }

        System.Diagnostics.Debug.WriteLine("GeeTest: no XAML host is available for the verification window.");
        return default;
    }
}
