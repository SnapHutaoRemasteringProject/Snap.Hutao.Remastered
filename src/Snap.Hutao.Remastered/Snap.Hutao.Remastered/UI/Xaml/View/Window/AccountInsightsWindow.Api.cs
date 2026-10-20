// Copyright (c) Snap Hutao RP. All rights reserved.
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.Core.Setting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public sealed partial class AccountInsightsWindow
{
    private static string CreateAnonymousJson(IEnumerable<InsightSeries> series)
    {
        JsonArray data = [];
        foreach (InsightSeries item in series)
        {
            JsonArray points = [];
            foreach (InsightPoint point in item.Points)
            {
                points.Add(new JsonObject { ["period"] = point.Period, ["value"] = point.Value });
            }

            double[] values = item.Points.Select(point => point.Value).ToArray();
            JsonObject summary = new()
            {
                ["recorded_period_count"] = values.Length,
                ["latest"] = item.IsTimeSeries && values.Length > 0 ? JsonValue.Create(values[^1]) : null,
                ["minimum"] = values.Length > 0 ? JsonValue.Create(values.Min()) : null,
                ["maximum"] = values.Length > 0 ? JsonValue.Create(values.Max()) : null,
                ["average"] = values.Length > 0 ? JsonValue.Create(Math.Round(values.Average(), 2)) : null,
                ["latest_change_from_previous"] = item.IsTimeSeries && values.Length > 1 ? JsonValue.Create(values[^1] - values[^2]) : null,
            };

            data.Add(new JsonObject
            {
                ["summary"] = summary,
                ["kind"] = item.IsTimeSeries ? "time_series" : "category_distribution",
                ["category"] = item.Title,
                ["metric"] = item.Metric,
                ["unit"] = item.Unit,
                ["direction"] = item.Direction switch
                {
                    InsightDirection.HigherIsBetter => "higher_is_better",
                    InsightDirection.LowerIsBetter => "lower_is_better",
                    _ => "neutral",
                },
                ["periods"] = points,
            });
        }

        return new JsonObject
        {
            ["schema_version"] = 1,
            ["scope"] = "Saved local history only; the most recent twelve periods are included for time-series charts; missing periods are not zero. Direct account identifiers are excluded, but history patterns may remain linkable.",
            ["series"] = data,
        }.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private void OnCopyClicked(object sender, RoutedEventArgs args)
    {
        if (snapshotJson is null)
        {
            return;
        }

        DataPackage package = new();
        package.SetText(snapshotJson);
        Clipboard.SetContent(package);
        StatusText.Text = L("Copied");
    }

    private async void OnTestClicked(object sender, RoutedEventArgs args)
    {
        await RequestAnalysisAsync("Reply with exactly the text OK, and nothing else. This is a connection test without account data.", true);
    }

    private async void OnGenerateClicked(object sender, RoutedEventArgs args)
    {
        if (snapshotJson is { } snapshot && hasRecordedData)
        {
            await RequestAnalysisAsync(snapshot, false);
        }
    }

    private async Task RequestAnalysisAsync(string userMessage, bool connectionTest)
    {
        string configuredEndpoint = EndpointBox.Text.Trim().TrimEnd('/');
        if (configuredEndpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            configuredEndpoint += "/chat/completions";
        }

        if (!Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out Uri? endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttps && !(endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback)) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            ResponseText.Text = L("InvalidEndpoint");
            return;
        }

        string model = ModelBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(model))
        {
            ResponseText.Text = L("MissingModel");
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiKeyBox.Password))
        {
            ResponseText.Text = L("MissingApiKey");
            return;
        }

        ActivityRing.IsActive = true;
        UpdateGenerateEnabled();
        TestButton.IsEnabled = false;
        ResponseText.Text = L("Sending");

        try
        {
            string language = cultureOptions.CurrentCulture.Value.EnglishName;
            string bcp47 = cultureOptions.CurrentCulture.Value.Name;

            string prompt = connectionTest
                ? "You are checking if a Chat Completions API works. Reply only OK."
                : $"""
                You are a careful Genshin Impact data analyst. Write exclusively in the application's interface language:
                {language} (BCP 47: {bcp47}). Use native, respectful idioms and never literally translate Chinese
                gaming slang like '欧皇' or '非酋' into other languages.

                Analyze the provided read-only, de-identified historical aggregates. The JSON may include
                hu_tao_computed_gacha_statistics produced by Snap Hutao's existing statistical calculator.
                These computed results take precedence over rough estimates from monthly totals.

                Structure the answer in substantive sections:
                1. Overall observed performance: summarize both strong and weak patterns, backed by actual numbers.
                2. Gacha luck: assess each banner pool separately, including observed pulls per five-star,
                   typical/higher/lower pity intervals, obtained five-stars, featured wins/losses, guaranteed outcomes
                   when explicitly recorded, and the amount of history observed. Describe luck qualitatively as
                   relatively favorable, ordinary, or unfavorable only where supported by enough observations.
                   Do not fabricate a global luck ranking, exact statistical percentile or confidence interval.
                3. Featured characters and weapons: mention *specific* acquired featured five-star names
                   from recorded_five_star_obtains and displayed historical banner names. Distinguish featured
                   rate-up from off-banner and distinguish a featured banner from actual acquisition.
                   Do not mistake four-star records for five-stars.
                4. Resource planning: discuss current pity, banner strategy and risks without assuming spending,
                   primogem inventory, ownership not present in the data, or future unannounced banners.
                5. Spiral Abyss: compare available star totals across recorded periods, identify trends and
                   plateaus without treating missing periods as zero.
                6. Imaginarium Theater and Stygian Onslaught: summarize medals, participation and solo
                   completion times. Shorter clear time is favorable; compare only compatible modes and periods.
                7. Practical priorities: give several concrete, appropriately cautious suggestions, with a
                   final short statement of data quality and limitations.

                IMPORTANT: There may be partial historic imports. Missing pulls and seasons are unknown, not zero.
                Ratios derived from truncated histories can be biased. Never guess absent character builds,
                team comps, artifact stats, player identity or unrelated private information.
                Category distributions are not chronological trends; don't confuse the two.
                Cite exact numbers from the JSON when making each judgment. Do not invent observations.
                """;

            JsonObject payload = new()
            {
                ["model"] = model,
                ["stream"] = false,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "system", ["content"] = prompt },
                    new JsonObject { ["role"] = "user", ["content"] = userMessage },
                },
            };

            using HttpClientHandler handler = new() { AllowAutoRedirect = false };
            using HttpClient client = new(handler) { Timeout = TimeSpan.FromSeconds(90) };
            using HttpRequestMessage request = new(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKeyBox.Password);

            using HttpResponseMessage response = await client.SendAsync(request, lifetime.Token);
            string body = await response.Content.ReadAsStringAsync(lifetime.Token);
            if (!response.IsSuccessStatusCode)
            {
                string errorCode = ExtractApiErrorCode(body);
                logger.LogWarning("Account insights API returned HTTP {Status} (provider code: {ProviderCode})",
                    (int)response.StatusCode, errorCode);

                string display = string.Format(cultureOptions.CurrentCulture.Value,
                    L("HttpError"), (int)response.StatusCode);
                if (errorCode.Length > 0)
                {
                    display += " [" + errorCode + "]";
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    display += " " + L("ApiUnauthorizedHint");
                }

                ResponseText.Text = display;
                return;
            }

            using JsonDocument parsed = JsonDocument.Parse(body);
            JsonElement choices = parsed.RootElement.GetProperty("choices");
            string? answer = choices[0].GetProperty("message").GetProperty("content").GetString();

            ResponseText.Text = connectionTest ? L("TestSuccess") : string.IsNullOrWhiteSpace(answer) ? L("EmptyReply") : answer;
            LocalSetting.Set(EndpointSettingKey, endpoint.ToString());
            LocalSetting.Set(ModelSettingKey, model);
        }
        catch (OperationCanceledException)
        {
            if (!lifetime.IsCancellationRequested)
            {
                ResponseText.Text = L("Timeout");
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Account insights API request failed");
            if (!lifetime.IsCancellationRequested)
            {
                ResponseText.Text = L("Failure");
            }
        }
        finally
        {
            if (!lifetime.IsCancellationRequested)
            {
                ActivityRing.IsActive = false;
                UpdateGenerateEnabled();
                TestButton.IsEnabled = true;
            }
        }
    }

    // Provider error codes are useful for diagnosis, but full HTTP response bodies
    // may echo user-provided credentials or request content. Never log or surface them.
    private static string ExtractApiErrorCode(string body)
    {
        try
        {
            using JsonDocument parsed = JsonDocument.Parse(body);
            JsonElement value = parsed.RootElement;
            if (value.TryGetProperty("error", out JsonElement nested) && nested.ValueKind == JsonValueKind.Object)
            {
                value = nested;
            }

            if (value.TryGetProperty("code", out JsonElement code))
            {
                string? text = code.ToString();
                if (text is not null && text.Length <= 80 && text.All(c => char.IsLetterOrDigit(c) || c is '_' or '-' or '.'))
                {
                    return text;
                }
            }
        }
        catch (JsonException)
        {
        }

        return string.Empty;
    }


}
