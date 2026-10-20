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
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AccountInsightsDialog> logger;
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
        ILogger<AccountInsightsDialog> logger,
        IGachaLogRepository gachaRepository,
        ISpiralAbyssRepository abyssRepository,
        IRoleCombatRepository combatRepository,
        IHardChallengeRepository challengeRepository)
    {
        InitializeComponent();
        this.cultureOptions = cultureOptions;
        this.logger = logger;
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
        return Translate(name, cultureOptions.CurrentCulture.Value);
    }

    private static string Translate(string name, CultureInfo culture)
    {
        return SH.ResourceManager.GetString("ViewDialogAccountInsights" + name, culture) ?? name;
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

            // Each repository has its own scope. Preserve healthy datasets if another fails.
            CultureInfo culture = cultureOptions.CurrentCulture.Value;
            InsightLoadResult result = await Task.Run(() => CreateSeries(userAndUid.Uid.Value, culture), lifetime.Token);
            lifetime.Token.ThrowIfCancellationRequested();

            hasRecordedData = result.Series.Any(item => item.Points.Count > 0);
            snapshotJson = hasRecordedData ? CreateAnonymousJson(result.Series) : null;

            ChartsPanel.Children.Clear();
            foreach (InsightSeries item in result.Series.Where(item => item.Points.Count > 0))
            {
                try
                {
                    ChartsPanel.Children.Add(CreateChart(item));
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to render account history chart: {Metric}", item.Metric);
                    ChartsPanel.Children.Add(CreateFallbackChart(item));
                }
            }

            string state = hasRecordedData ? L("Ready") : L("NoRecords");
            if (result.FailedSources.Count > 0)
            {
                state += " " + L("PartialError") + ": " +
                    string.Join(", ", result.FailedSources.Select(source => L(source)));
            }

            StatusText.Text = state;
            CopyButton.IsEnabled = hasRecordedData;
            UpdateGenerateEnabled();
        }
        catch (OperationCanceledException)
        {
            // The dialog was closed while the initial load was running.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to initialize account insights");
            StatusText.Text = L("ReadError");
        }
    }

    private static string FormatScheduleDate(Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DateTime time)
    {
        return $"{time.Year:D4}-{time.Month:D2}-{time.Day:D2}";
    }

    // A bad saved season must not prevent the other game modes from loading.
    private List<T> ReadSafely<T>(string source, Func<List<T>> read, List<string> failures)
    {
        try
        {
            return read();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Account insights could not load {Source}", source);
            failures.Add(source);
            return [];
        }
    }

    private InsightLoadResult CreateSeries(string uid, CultureInfo culture)
    {
        List<string> failures = [];

        List<GachaItem> gachaItems = ReadSafely("ChartWish", () =>
            gachaRepository.GetGachaArchiveByUid(uid) is { } archive
                ? gachaRepository.GetGachaItemImmutableArrayByArchiveId(archive.InnerId).ToList()
                : [], failures);

        List<InsightPoint> monthlyWishes = ReadSafely("ChartWish", () => gachaItems
            .GroupBy(item => item.Time.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new InsightPoint(group.Key, group.Count()))
            .TakeLast(12)
            .ToList(), failures);

        List<InsightPoint> poolWishes = ReadSafely("ChartWish", () => gachaItems
            .GroupBy(item => item.QueryType)
            .OrderBy(group => (int)group.Key)
            .Select(group => new InsightPoint(GetWishPoolLabel(group.Key), group.Count()))
            .ToList(), failures);

        List<BeyondGachaItem> beyondItems = ReadSafely("ChartBeyond", () =>
            gachaRepository.GetGachaArchiveByUid(uid) is { } beyondArchive
                ? gachaRepository.GetBeyondGachaItemImmutableArrayByArchiveId(beyondArchive.InnerId).ToList()
                : [], failures);

        List<InsightPoint> monthlyBeyondWishes = ReadSafely("ChartBeyond", () => beyondItems
            .GroupBy(item => item.Time.ToString("yyyy-MM", CultureInfo.InvariantCulture))
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new InsightPoint(group.Key, group.Count()))
            .TakeLast(12)
            .ToList(), failures);

        List<InsightPoint> beyondPoolWishes = ReadSafely("ChartBeyond", () => beyondItems
            .GroupBy(item => item.QueryType)
            .OrderBy(group => (int)group.Key)
            .Select(group => new InsightPoint(GetWishPoolLabel(group.Key), group.Count()))
            .ToList(), failures);

        List<InsightPoint> abyss = ReadSafely("ChartAbyss", () => abyssRepository.GetSpiralAbyssEntryMapByUid(uid).Values
            .Where(entry => entry.SpiralAbyss is { IsUnlock: true })
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(
                entry.SpiralAbyss.StartTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(entry.SpiralAbyss.StartTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : entry.ScheduleId.ToString(CultureInfo.InvariantCulture),
                entry.SpiralAbyss.TotalStar))
            .TakeLast(12)
            .ToList(), failures);

        List<InsightPoint> theater = ReadSafely("ChartTheater", () => combatRepository.GetRoleCombatEntryMapByUid(uid).Values
            .Where(entry => entry.RoleCombatData is { HasData: true, Schedule: not null, Stat: not null })
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(
                FormatScheduleDate(entry.RoleCombatData.Schedule.StartDateTime),
                entry.RoleCombatData.Stat.MedalNumber))
            .TakeLast(12)
            .ToList(), failures);

        // Solo and multiplayer have different conditions; avoid mixing their records.
        List<InsightPoint> challenge = ReadSafely("ChartChallenge", () => challengeRepository.GetHardChallengeMapByUid(uid).Values
            .Where(entry => entry.HardChallengeData?.Schedule is not null
                && entry.HardChallengeData.SinglePlayer is { HasData: true, Best: not null })
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(
                FormatScheduleDate(entry.HardChallengeData.Schedule.StartDateTime),
                entry.HardChallengeData.SinglePlayer.Best!.Seconds))
            .TakeLast(12)
            .ToList(), failures);

        List<InsightSeries> series =
        [
            new(Translate("ChartWish", culture), Translate("MetricMonthly", culture), Translate("UnitWish", culture), monthlyWishes, InsightDirection.Neutral),
            new(Translate("ChartWish", culture), Translate("MetricPool", culture), Translate("UnitWish", culture), poolWishes, InsightDirection.Neutral, false),
            new(Translate("ChartBeyond", culture), Translate("MetricMonthly", culture), Translate("UnitWish", culture), monthlyBeyondWishes, InsightDirection.Neutral),
            new(Translate("ChartBeyond", culture), Translate("MetricPool", culture), Translate("UnitWish", culture), beyondPoolWishes, InsightDirection.Neutral, false),
            new(Translate("ChartAbyss", culture), Translate("MetricAbyss", culture), Translate("UnitStar", culture), abyss, InsightDirection.HigherIsBetter),
            new(Translate("ChartTheater", culture), Translate("MetricTheater", culture), Translate("UnitMedal", culture), theater, InsightDirection.HigherIsBetter),
            new(Translate("ChartChallenge", culture), Translate("MetricChallenge", culture), Translate("UnitSeconds", culture), challenge, InsightDirection.LowerIsBetter),
        ];
        return new InsightLoadResult(series, failures.Distinct().ToList());
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
            FontSize = 18,
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

        // Experimental charts can fail at runtime on specific Windows App SDK builds.
        // A light-weight fallback keeps both the history and LLM feature available.
        try
        {
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
                Label = series.Metric + " (" + series.Unit + ")",
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
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Native WinUI chart failed: {Metric}", series.Metric);
            AddFallbackBars(panel, series);
        }

        return panel;
    }

    private UIElement CreateFallbackChart(InsightSeries series)
    {
        StackPanel panel = new() { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = series.Title + " · " + series.Metric, FontSize = 18 });
        AddFallbackBars(panel, series);
        return panel;
    }

    private void AddFallbackBars(StackPanel panel, InsightSeries series)
    {
        if (series.Points.Count == 0)
        {
            panel.Children.Add(new TextBlock { Text = L("NoData") });
            return;
        }

        double maximum = Math.Max(1, series.Points.Max(point => point.Value));
        foreach (InsightPoint point in series.Points)
        {
            Grid row = new() { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            TextBlock label = new() { Text = point.Period, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            ProgressBar bar = new() { Maximum = maximum, Value = point.Value, Height = 12, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(bar, 1);
            TextBlock value = new()
            {
                Text = point.Value.ToString("0.##", cultureOptions.CurrentCulture.Value) + " " + series.Unit,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(value, 2);
            row.Children.Add(label);
            row.Children.Add(bar);
            row.Children.Add(value);
            panel.Children.Add(row);
        }
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

    private async void OnGenerateClicked(object sender, RoutedEventArgs args)
    {
        if (snapshotJson is null || !hasRecordedData)
        {
            return;
        }

        // Accept either the complete Chat Completions endpoint or a provider's API base URL.
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

    private sealed record InsightLoadResult(List<InsightSeries> Series, List<string> FailedSources);

    private sealed record InsightPoint(string Period, double Value);

    private sealed record InsightSeries(
        string Title,
        string Metric,
        string Unit,
        List<InsightPoint> Points,
        InsightDirection Direction,
        bool IsTimeSeries = true);
}
