// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Service.GachaLog;
using Snap.Hutao.Remastered.Service.HardChallenge;
using Snap.Hutao.Remastered.Service.RoleCombat;
using Snap.Hutao.Remastered.Service.SpiralAbyss;
using Snap.Hutao.Remastered.Service.User;
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
/// Displays read-only, per-account historical aggregates. Only anonymous aggregates can
/// enter the optional user-initiated LLM request; repositories are never exposed to the model.
/// </summary>
public sealed class AccountInsightsDialog : ContentDialog
{
    private const string EndpointSettingKey = "AccountInsightsLlmEndpoint";
    private const string ModelSettingKey = "AccountInsightsLlmModel";

    private readonly IUserService userService;
    private readonly IGachaLogRepository gachaRepository;
    private readonly ISpiralAbyssRepository abyssRepository;
    private readonly IRoleCombatRepository combatRepository;
    private readonly IHardChallengeRepository challengeRepository;
    private readonly CancellationTokenSource lifetime = new();

    private readonly StackPanel charts = new() { Spacing = 20 };
    private readonly TextBlock status = new() { Text = "正在读取本地历史记录…", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock responseText = new() { TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
    private readonly TextBox endpointBox = new()
    {
        Header = "Chat Completions API 地址（仅 HTTPS，或本机 HTTP）",
        PlaceholderText = "https://example.com/v1/chat/completions",
    };
    private readonly TextBox modelBox = new() { Header = "模型名称", PlaceholderText = "模型标识符" };
    private readonly PasswordBox apiKeyBox = new() { Header = "API Key（仅本次使用，不保存）" };
    private readonly CheckBox consentBox = new()
    {
        Content = "我同意将下方匿名汇总统计发送到上述第三方 API 服务",
        IsChecked = false,
    };
    private readonly Button generateButton = new() { Content = "发送统计并生成分析评价", IsEnabled = false };
    private readonly Button copyButton = new() { Content = "复制匿名统计 JSON", IsEnabled = false };
    private readonly ProgressRing activityRing = new() { IsActive = false, Width = 26, Height = 26 };

    private string? snapshotJson;

    public AccountInsightsDialog(
        IUserService userService,
        IGachaLogRepository gachaRepository,
        ISpiralAbyssRepository abyssRepository,
        IRoleCombatRepository combatRepository,
        IHardChallengeRepository challengeRepository)
    {
        this.userService = userService;
        this.gachaRepository = gachaRepository;
        this.abyssRepository = abyssRepository;
        this.combatRepository = combatRepository;
        this.challengeRepository = challengeRepository;

        Title = "账号历史图表与 AI 分析";
        CloseButtonText = "关闭";
        DefaultButton = ContentDialogButton.Close;
        MinWidth = 600;
        MaxWidth = 860;

        endpointBox.Text = LocalSetting.Get(EndpointSettingKey, string.Empty);
        modelBox.Text = LocalSetting.Get(ModelSettingKey, string.Empty);

        StackPanel layout = new() { Spacing = 14, Padding = new Thickness(4, 8, 4, 8) };
        layout.Children.Add(new TextBlock
        {
            Text = "图表依据胡桃本地已保存的记录生成。未保存或尚未同步的周期不会被估算。",
            TextWrapping = TextWrapping.Wrap,
        });
        layout.Children.Add(status);
        layout.Children.Add(charts);
        layout.Children.Add(copyButton);
        layout.Children.Add(new TextBlock
        {
            Text = "可选 AI 评价：只发送以下图表的数值摘要，不发送 UID、祈愿物品 ID、Cookie 或数据库文件。",
            TextWrapping = TextWrapping.Wrap,
        });
        layout.Children.Add(endpointBox);
        layout.Children.Add(modelBox);
        layout.Children.Add(apiKeyBox);
        layout.Children.Add(consentBox);
        StackPanel actions = new() { Orientation = Orientation.Horizontal, Spacing = 12 };
        actions.Children.Add(generateButton);
        actions.Children.Add(activityRing);
        layout.Children.Add(actions);
        layout.Children.Add(responseText);

        Content = new ScrollViewer
        {
            Height = 540,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = layout,
        };

        consentBox.Checked += (_, _) => UpdateGenerateEnabled();
        consentBox.Unchecked += (_, _) => UpdateGenerateEnabled();
        copyButton.Click += OnCopyClicked;
        generateButton.Click += OnGenerateClicked;
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            lifetime.Cancel();
            apiKeyBox.Password = string.Empty;
        };
    }

    private void UpdateGenerateEnabled()
    {
        generateButton.IsEnabled = snapshotJson is not null && consentBox.IsChecked is true && !activityRing.IsActive;
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        try
        {
            if (await userService.GetCurrentUserAndUidAsync() is not { } userAndUid)
            {
                status.Text = "请先选择游戏账号及 UID。";
                return;
            }

            // EF repositories are synchronous: keep database reads off the UI dispatcher.
            List<InsightSeries> series = await Task.Run(() => CreateSeries(userAndUid.Uid.Value), lifetime.Token);
            lifetime.Token.ThrowIfCancellationRequested();

            snapshotJson = CreateAnonymousJson(series);
            charts.Children.Clear();
            foreach (InsightSeries item in series)
            {
                charts.Children.Add(CreateChart(item));
            }

            status.Text = series.All(item => item.Points.Count == 0)
                ? "所选账号尚无可绘制的本地记录。"
                : "已读取四类统计。每条横条代表一个月份或活动周期，数值来源于本地已保存记录。";
            copyButton.IsEnabled = true;
            UpdateGenerateEnabled();
        }
        catch (OperationCanceledException)
        {
            // The dialog was closed while the initial load was running.
        }
        catch (Exception)
        {
            status.Text = "读取历史记录失败。请检查账号或本地数据是否完整。";
        }
    }

    private List<InsightSeries> CreateSeries(string uid)
    {
        List<InsightPoint> wishes = [];
        if (gachaRepository.GetGachaArchiveByUid(uid) is { } archive)
        {
            wishes = gachaRepository.GetGachaItemImmutableArrayByArchiveId(archive.InnerId)
                .GroupBy(item => item.Time.ToString("yyyy-MM", CultureInfo.InvariantCulture))
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new InsightPoint(group.Key, group.Count()))
                .TakeLast(24)
                .ToList();
        }

        List<InsightPoint> abyss = abyssRepository.GetSpiralAbyssEntryMapByUid(uid).Values
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(entry.ScheduleId.ToString(CultureInfo.InvariantCulture), entry.SpiralAbyss.TotalStar))
            .TakeLast(16)
            .ToList();

        List<InsightPoint> theater = combatRepository.GetRoleCombatEntryMapByUid(uid).Values
            .Where(entry => entry.RoleCombatData.HasData)
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(entry.ScheduleId.ToString(CultureInfo.InvariantCulture), entry.RoleCombatData.Stat.MedalNumber))
            .TakeLast(16)
            .ToList();

        // Compare solo results only. Mixing multiplayer and solo times would be misleading.
        List<InsightPoint> challenge = challengeRepository.GetHardChallengeMapByUid(uid).Values
            .Where(entry => entry.HardChallengeData.SinglePlayer.HasData && entry.HardChallengeData.SinglePlayer.Best is not null)
            .OrderBy(entry => entry.ScheduleId)
            .Select(entry => new InsightPoint(entry.ScheduleId.ToString(CultureInfo.InvariantCulture), entry.HardChallengeData.SinglePlayer.Best!.Seconds))
            .TakeLast(16)
            .ToList();

        return
        [
            new("祈愿记录", "每月抽数", "抽", wishes, false),
            new("深境螺旋", "每期星数", "星", abyss, false),
            new("幻想真境剧诗", "每期奖章数", "枚", theater, false),
            new("幽境危战（单人）", "每期最佳通关用时", "秒", challenge, true),
        ];
    }

    private static UIElement CreateChart(InsightSeries series)
    {
        StackPanel panel = new() { Spacing = 6 };
        panel.Children.Add(new TextBlock
        {
            Text = series.Title + " · " + series.Metric + (series.LowerIsBetter ? "（越少越好）" : ""),
            Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style,
        });

        if (series.Points.Count == 0)
        {
            panel.Children.Add(new TextBlock { Text = "没有已保存的记录。" });
            return panel;
        }

        double maximum = Math.Max(1, series.Points.Max(point => point.Value));
        foreach (InsightPoint point in series.Points)
        {
            StackPanel row = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new TextBlock { Text = point.Period, Width = 110, VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new ProgressBar
            {
                Width = 240,
                Maximum = maximum,
                Value = point.Value,
                Height = 12,
                VerticalAlignment = VerticalAlignment.Center,
            });
            row.Children.Add(new TextBlock
            {
                Text = point.Value.ToString("0.##", CultureInfo.InvariantCulture) + " " + series.Unit,
                VerticalAlignment = VerticalAlignment.Center,
            });
            panel.Children.Add(row);
        }

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

            data.Add(new JsonObject
            {
                ["category"] = item.Title,
                ["metric"] = item.Metric,
                ["unit"] = item.Unit,
                ["lower_is_better"] = item.LowerIsBetter,
                ["periods"] = points,
            });
        }

        return new JsonObject
        {
            ["schema_version"] = 1,
            ["scope"] = "Saved local history only; missing periods are not equivalent to a score of zero.",
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
        status.Text = "已复制匿名统计数据。此操作不会修改胡桃数据库。";
    }

    private async void OnGenerateClicked(object sender, RoutedEventArgs args)
    {
        if (snapshotJson is null || consentBox.IsChecked is not true)
        {
            return;
        }

        if (!Uri.TryCreate(endpointBox.Text.Trim(), UriKind.Absolute, out Uri? endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttps && !(endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback)) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            responseText.Text = "API 地址必须是完整的 HTTPS URL（仅本机回环地址允许 HTTP）。";
            return;
        }

        if (string.IsNullOrWhiteSpace(modelBox.Text))
        {
            responseText.Text = "请输入模型名称。";
            return;
        }

        activityRing.IsActive = true;
        UpdateGenerateEnabled();
        responseText.Text = "正在请求模型分析…";
        try
        {
            JsonArray messages =
            [
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = "你是游戏记录分析助手。只根据给出的匿名历史统计评价趋势；必须区分观察事实和建议。不可推测用户身份、角色培养、装备或运气好坏，不可假定未记录周期为零分。特别注意不同指标方向，以及样本量较小时的不确定性。使用中文，简洁而具体。",
                },
                new JsonObject { ["role"] = "user", ["content"] = snapshotJson },
            ];

            JsonObject payload = new()
            {
                ["model"] = modelBox.Text.Trim(),
                ["messages"] = messages,
                ["stream"] = false,
            };

            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(60) };
            using HttpRequestMessage request = new(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
            };

            if (!string.IsNullOrWhiteSpace(apiKeyBox.Password))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKeyBox.Password);
            }

            using HttpResponseMessage response = await client.SendAsync(request, lifetime.Token);
            if (!response.IsSuccessStatusCode)
            {
                responseText.Text = "API 请求失败，HTTP 状态：" + (int)response.StatusCode + "。请检查地址、模型及权限。";
                return;
            }

            string body = await response.Content.ReadAsStringAsync(lifetime.Token);
            using JsonDocument parsed = JsonDocument.Parse(body);
            JsonElement choices = parsed.RootElement.GetProperty("choices");
            string? answer = choices[0].GetProperty("message").GetProperty("content").GetString();
            responseText.Text = string.IsNullOrWhiteSpace(answer) ? "模型未返回文字内容。" : answer;
            LocalSetting.Set(EndpointSettingKey, endpoint.ToString());
            LocalSetting.Set(ModelSettingKey, modelBox.Text.Trim());
        }
        catch (OperationCanceledException)
        {
            if (!lifetime.IsCancellationRequested)
            {
                responseText.Text = "请求已取消或超时。";
            }
        }
        catch (Exception)
        {
            responseText.Text = "分析失败：响应格式或网络连接异常。密钥未保存。";
        }
        finally
        {
            if (!lifetime.IsCancellationRequested)
            {
                activityRing.IsActive = false;
                UpdateGenerateEnabled();
            }
        }
    }

    private sealed record InsightPoint(string Period, double Value);

    private sealed record InsightSeries(
        string Title,
        string Metric,
        string Unit,
        List<InsightPoint> Points,
        bool LowerIsBetter);
}
