// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Charts;
using ChartSamples = Microsoft.UI.Xaml.Controls.Charts.Samples;
using WinUIChart = Microsoft.UI.Xaml.Controls.Charts.Chart;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.GachaLog;
using Snap.Hutao.Remastered.Service.HardChallenge;
using Snap.Hutao.Remastered.Service.RoleCombat;
using Snap.Hutao.Remastered.Service.SpiralAbyss;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

/// <summary>
/// Displays read-only, per-account historical aggregates. Only de-identified aggregates can
/// enter the optional user-initiated LLM request; repositories are never exposed to the model.
/// </summary>
public sealed partial class AccountInsightsDialog : ContentDialog
{
    private const string EndpointSettingKey = "AccountInsightsLlmEndpoint";
    private const string ModelSettingKey = "AccountInsightsLlmModel";

    private readonly CultureOptions cultureOptions;
    private readonly IUserService userService;
    private readonly IGachaLogRepository gachaRepository;
    private readonly ISpiralAbyssRepository abyssRepository;
    private readonly IRoleCombatRepository combatRepository;
    private readonly IHardChallengeRepository challengeRepository;
    private readonly CancellationTokenSource lifetime = new();

    private string? snapshotJson;
    private bool hasRecordedData;

    public AccountInsightsDialog(
        IUserService userService,
        CultureOptions cultureOptions,
        IGachaLogRepository gachaRepository,
        ISpiralAbyssRepository abyssRepository,
        IRoleCombatRepository combatRepository,
        IHardChallengeRepository challengeRepository)
    {
        InitializeComponent();
        this.cultureOptions = cultureOptions;
        this.userService = userService;
        this.gachaRepository = gachaRepository;
        this.abyssRepository = abyssRepository;
        this.combatRepository = combatRepository;
        this.challengeRepository = challengeRepository;

        MinWidth = 600;
        MaxWidth = 860;
        EndpointBox.Text = LocalSetting.Get(EndpointSettingKey, string.Empty);
        ModelBox.Text = LocalSetting.Get(ModelSettingKey, string.Empty);

        PrivacyInfoButton.Click += (_, _) => PrivacyTeachingTip.IsOpen = true;
        CopyButton.Click += OnCopyClicked;
        GenerateButton.Click += OnGenerateClicked;
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            lifetime.Cancel();
            PrivacyTeachingTip.IsOpen = false;
            ApiKeyBox.Password = string.Empty;
        };
    }

    private string L(string name)
    {
        return SH.ResourceManager.GetString("ViewDialogAccountInsights" + name, cultureOptions.CurrentCulture.Value) ?? name;
    }

    private void UpdateGenerateEnabled()
    {
        GenerateButton.IsEnabled = hasRecordedData && snapshotJson is not null && !ActivityRing.IsActive;
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        try
        {
            if (await userService.GetCurrentUserAndUidAsync() is not { } userAndUid)
            {
                StatusText.Text = L("NoAccount");
                return;
            }

            // EF repositories are synchronous: keep database reads off the UI dispatcher.
            List<InsightSeries> series = await Task.Run(() => CreateSeries(userAndUid.Uid.Value), lifetime.Token);
            lifetime.Token.ThrowIfCancellationRequested();

            hasRecordedData = series.Any(item => item.Points.Count > 0);
            snapshotJson = CreateAnonymousJson(series);
            ChartsPanel.Children.Clear();
            foreach (InsightSeries item in series)
            {
                ChartsPanel.Children.Add(CreateChart(item));
            }

            StatusText.Text = !hasRecordedData
                ? L("NoRecords")
                : L("Ready");
            CopyButton.IsEnabled = true;
            UpdateGenerateEnabled();
        }
        catch (OperationCanceledException)
        {
            // The dialog was closed while the initial load was running.
        }
        catch (Exception)
        {
            StatusText.Text = L("ReadError");
        }
    }

    private static string FormatScheduleDate(Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DateTime time)
    {
        return $"{time.Year:D4}-{time.Month:D2}-{time.Day:D2}";
    }

    private List<InsightSeries> CreateSeries(string uid)
    {
        List<GachaItem> gachaItems = gachaRepository.GetGachaArchiveByUid(uid) is { } archive
            ? gachaRepository.GetGachaItemImmutableArrayByArchiveId(archive.InnerId).ToList()
            : [];

        List<InsightPoint> monthlyWishes = gachaItems
            .GroupBy(item => item.Time.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new InsightPoint(group.Key, group.Count()))
            .TakeLast(12)
            .ToList();

        // QueryType is the actual pool category used by the existing wish statistics.
        // Do not combine unrelated pity pools, or infer rarity from an item ID.
        List<InsightPoint> poolWishes = gachaItems
            .GroupBy(item => item.QueryType)
            .OrderBy(group => (int)group.Key)
            .Select(group => new InsightPoint(GetWishPoolLabel(group.Key), group.Count()))
            .ToList();

        List<BeyondGachaItem> beyondItems = gachaRepository.GetGachaArchiveByUid(uid) is { } beyondArchive
            ? gachaRepository.GetBeyondGachaItemImmutableArrayByArchiveId(beyondArchive.InnerId).ToList()
            : [];

        List<InsightPoint> monthlyBeyondWishes = beyondItems
            .GroupBy(item => item.Time.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new InsightPoint(group.Key, group.Count()))
            .TakeLast(12)
            .ToList();

        List<InsightPoint> beyondPoolWishes = beyondItems
            .GroupBy(item => item.QueryType)
            .OrderBy(group => (int)group.Key)
            .Select(group => new InsightPoint(GetWishPoolLabel(group.Key), group.Count()))
            .ToList();

        List<InsightPoint> abyss = abyssRepository.GetSpiralAbyssEntryMapByUid(uid).Values
            .Where(entry => entry.SpiralAbyss.IsUnlock)
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(
                entry.SpiralAbyss.StartTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(entry.SpiralAbyss.StartTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : entry.ScheduleId.ToString(CultureInfo.InvariantCulture),
                entry.SpiralAbyss.TotalStar))
            .TakeLast(12)
            .ToList();

        List<InsightPoint> theater = combatRepository.GetRoleCombatEntryMapByUid(uid).Values
            .Where(entry => entry.RoleCombatData.HasData)
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(
                FormatScheduleDate(entry.RoleCombatData.Schedule.StartDateTime),
                entry.RoleCombatData.Stat.MedalNumber))
            .TakeLast(12)
            .ToList();

        // Solo and multiplayer records have different conditions and must not be compared.
        List<InsightPoint> challenge = challengeRepository.GetHardChallengeMapByUid(uid).Values
            .Where(entry => entry.HardChallengeData.SinglePlayer.HasData && entry.HardChallengeData.SinglePlayer.Best is not null)
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(
                FormatScheduleDate(entry.HardChallengeData.Schedule.StartDateTime),
                entry.HardChallengeData.SinglePlayer.Best!.Seconds))
            .TakeLast(12)
            .ToList();

        return
        [
            new(L("ChartWish"), L("MetricMonthly"), L("UnitWish"), monthlyWishes, InsightDirection.Neutral),
            new(L("ChartWish"), L("MetricPool"), L("UnitWish"), poolWishes, InsightDirection.Neutral, false),
            new(L("ChartBeyond"), L("MetricMonthly"), L("UnitWish"), monthlyBeyondWishes, InsightDirection.Neutral),
            new(L("ChartBeyond"), L("MetricPool"), L("UnitWish"), beyondPoolWishes, InsightDirection.Neutral, false),
            new(L("ChartAbyss"), L("MetricAbyss"), L("UnitStar"), abyss, InsightDirection.HigherIsBetter),
            new(L("ChartTheater"), L("MetricTheater"), L("UnitMedal"), theater, InsightDirection.HigherIsBetter),
            new(L("ChartChallenge"), L("MetricChallenge"), L("UnitSeconds"), challenge, InsightDirection.LowerIsBetter),
        ];
    }

    private static string GetWishPoolLabel(GachaType type)
    {
        return type switch
        {
            GachaType.NewBie => SH.WebGachaConfigTypeNoviceWish,
            GachaType.Standard => SH.WebGachaConfigTypePermanentWish,
            GachaType.ActivityAvatar => SH.WebGachaConfigTypeAvatarEventWish,
            GachaType.ActivityWeapon => SH.WebGachaConfigTypeWeaponEventWish,
            GachaType.SpecialActivityAvatar => SH.WebGachaConfigTypeAvatarEventWish2,
            GachaType.ActivityCity => SH.WebGachaConfigTypeChronicledWish,
            GachaType.UGCStandard => SH.WebUGCGachaConfigTypePermanentWish,
            GachaType.UGCAvatarEventWish => SH.WebUGCGachaConfigTypeAvatarEventWish,
            GachaType.UGCActivityAvatarMaleOne => SH.WebUGCGachaConfigTypeActivityAvatarMaleOne,
            GachaType.UGCActivityAvatarMaleTwo => SH.WebUGCGachaConfigTypeActivityAvatarMaleTwo,
            GachaType.UGCActivityAvatarFemaleOne => SH.WebUGCGachaConfigTypeActivityAvatarFemaleOne,
            GachaType.UGCActivityAvatarFemaleTwo => SH.WebUGCGachaConfigTypeActivityAvatarFemaleTwo,
            _ => type.ToString(),
        };
    }

    private UIElement CreateChart(InsightSeries series)
    {
        StackPanel panel = new() { Spacing = 6 };
        panel.Children.Add(new TextBlock
        {
            Text = series.Title + " · " + series.Metric + (series.Direction == InsightDirection.LowerIsBetter ? " (" + L("LowerIsBetter") + ")" : ""),
            Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style,
        });

        if (series.Points.Count == 0)
        {
            panel.Children.Add(new TextBlock { Text = L("NoData") });
            return panel;
        }

        double mean = series.Points.Average(point => point.Value);
        InsightPoint newest = series.Points[^1];
        CultureInfo culture = cultureOptions.CurrentCulture.Value;
        string summary;
        if (series.IsTimeSeries)
        {
            summary = string.Format(culture, L("SummaryTime"), series.Points.Count, mean.ToString("0.##", culture),
                newest.Value.ToString("0.##", culture), series.Unit);
            if (series.Points.Count > 1)
            {
                string change = (newest.Value - series.Points[^2].Value).ToString("+0.##;-0.##;0", culture);
                summary += string.Format(culture, L("SummaryDelta"), change, series.Unit);
            }
        }
        else
        {
            summary = string.Format(culture, L("SummaryPool"), series.Points.Count,
                series.Points.Sum(point => point.Value).ToString("0", culture), series.Unit);
        }

        panel.Children.Add(new TextBlock { Text = summary, TextWrapping = TextWrapping.Wrap });

        // Experimental Microsoft.UI.Xaml.Controls.Charts is already present in WASDK 2.5.4.
        // Use independent axis/sample objects for each series to avoid shared control state.
        ChartSamples periods = new() { ItemsSource = series.Points.Select(point => point.Period).ToArray() };
        ChartSamples values = new() { ItemsSource = series.Points.Select(point => point.Value).ToArray() };
        CategoryAxis periodAxis = new()
        {
            Label = L("PeriodAxis"),
            SortKey = CategorySortKey.Index,
            SortOrder = SortOrder.Ascending,
        };
        LinearAxis metricAxis = new()
        {
            Label = series.Metric + "（" + series.Unit + "）",
            Minimum = 0,
            GridLines = GridLines.Major,
        };

        WinUIChart chart = new() { Height = 300, ShowLegend = false };
        chart.Data.Add(periods);
        chart.Data.Add(values);
        chart.Axes.Add(periodAxis);
        chart.Axes.Add(metricAxis);
        chart.Series.Add(new BarSeries
        {
            Title = series.Metric,
            Orientation = BarOrientation.Vertical,
            XAxis = periodAxis,
            XValues = periods,
            YAxis = metricAxis,
            YValues = values,
            ShowDataLabels = true,
        });

        panel.Children.Add(chart);
        return panel;
    }

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
            ["scope"] = "Saved local history only; the most recent twelve periods are included for time-series ChartsPanel; missing periods are not zero. Direct account identifiers are excluded, but history patterns may remain linkable.",
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

    private async void OnGenerateClicked(object sender, RoutedEventArgs args)
    {
        if (snapshotJson is null || !hasRecordedData)
        {
            return;
        }

        if (!Uri.TryCreate(EndpointBox.Text.Trim(), UriKind.Absolute, out Uri? endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttps && !(endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback)) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            ResponseText.Text = L("InvalidEndpoint");
            return;
        }

        if (string.IsNullOrWhiteSpace(ModelBox.Text))
        {
            ResponseText.Text = L("MissingModel");
            return;
        }

        ActivityRing.IsActive = true;
        UpdateGenerateEnabled();
        ResponseText.Text = L("Sending");
        try
        {
            JsonArray messages =
            [
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = $"You analyze Genshin Impact history data. Reply exclusively in the application interface language: {cultureOptions.CurrentCulture.Value.EnglishName} (BCP 47: {cultureOptions.CurrentCulture.Value.Name}). Use only the supplied anonymized historical aggregates. Separate observations, limitations and suggestions. Never invent missing periods or player data and never treat absent periods as zero. Take account of units, direction of improvements and sample sizes. Do not infer identity, spending, builds, weapon ownership or character strength from counts alone. Do not compare neighboring category labels as chronological periods. Keep the assessment concise and actionable.",
                },
                new JsonObject { ["role"] = "user", ["content"] = snapshotJson },
            ];

            JsonObject payload = new()
            {
                ["model"] = ModelBox.Text.Trim(),
                ["messages"] = messages,
                ["stream"] = false,
            };

            using HttpClientHandler handler = new() { AllowAutoRedirect = false };
            using HttpClient client = new(handler) { Timeout = TimeSpan.FromSeconds(60) };
            using HttpRequestMessage request = new(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
            };

            if (!string.IsNullOrWhiteSpace(ApiKeyBox.Password))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKeyBox.Password);
            }

            using HttpResponseMessage response = await client.SendAsync(request, lifetime.Token);
            if (!response.IsSuccessStatusCode)
            {
                ResponseText.Text = string.Format(cultureOptions.CurrentCulture.Value, L("HttpError"), (int)response.StatusCode);
                return;
            }

            string body = await response.Content.ReadAsStringAsync(lifetime.Token);
            using JsonDocument parsed = JsonDocument.Parse(body);
            JsonElement choices = parsed.RootElement.GetProperty("choices");
            string? answer = choices[0].GetProperty("message").GetProperty("content").GetString();
            ResponseText.Text = string.IsNullOrWhiteSpace(answer) ? L("EmptyReply") : answer;
            LocalSetting.Set(EndpointSettingKey, endpoint.ToString());
            LocalSetting.Set(ModelSettingKey, ModelBox.Text.Trim());
        }
        catch (OperationCanceledException)
        {
            if (!lifetime.IsCancellationRequested)
            {
                ResponseText.Text = L("Timeout");
            }
        }
        catch (Exception)
        {
            ResponseText.Text = L("Failure");
        }
        finally
        {
            if (!lifetime.IsCancellationRequested)
            {
                ActivityRing.IsActive = false;
                UpdateGenerateEnabled();
            }
        }
    }

    private enum InsightDirection
    {
        Neutral,
        HigherIsBetter,
        LowerIsBetter,
    }

    private sealed record InsightPoint(string Period, double Value);

    private sealed record InsightSeries(
        string Title,
        string Metric,
        string Unit,
        List<InsightPoint> Points,
        InsightDirection Direction,
        bool IsTimeSeries = true);
}
