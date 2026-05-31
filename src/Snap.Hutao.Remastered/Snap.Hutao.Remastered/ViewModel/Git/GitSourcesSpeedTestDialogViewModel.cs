// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Snap.Hutao.Remastered.Service.Git;
using Snap.Hutao.Remastered.Web.Hutao;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using Snap.Hutao.Remastered.Web.Hutao.Response;
using System.Globalization;
using Snap.Hutao.Remastered.Service;

namespace Snap.Hutao.Remastered.ViewModel.Git;

[BindableCustomPropertyProvider]
internal sealed partial class GitSourcesSpeedTestDialogViewModel : Abstraction.ViewModel
{
    private readonly GitMirrorSpeedTester? tester;
    private readonly IServiceProvider serviceProvider;
    private readonly AppOptions appOptions;
    private readonly ITaskContext taskContext;

    public ObservableCollection<MirrorTestResult> TestResults { get; } = new();

    [ObservableProperty]
    public partial bool IsRunning { get; private set; }

    [ObservableProperty]
    public partial string StatusMessage { get; private set; }

    [ObservableProperty]
    public partial MirrorTestResult? SelectedTestResult { get; private set; }

    public GitSourcesSpeedTestDialogViewModel(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        tester = serviceProvider.GetService<GitMirrorSpeedTester>();
        appOptions = serviceProvider.GetRequiredService<AppOptions>();
        taskContext = serviceProvider.GetRequiredService<ITaskContext>();
        StatusMessage = SH.ViewDialogGitSourcesSpeedTestDialogInitialStatusMessage;
    }

    [Command("RunCommand")]
    public async Task RunSpeedTestAsync()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        StatusMessage = SH.ViewDialogGitSourcesSpeedTestDialogFetchingMirrorListStatusMessage;
        TestResults.Clear();

        DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            HutaoInfrastructureClient client = scope.ServiceProvider.GetRequiredService<HutaoInfrastructureClient>();

            HutaoResponse<ImmutableArray<GitRepository>> response = await client.GetGitRepositoryAsync("Snap.Metadata").ConfigureAwait(false);
            ImmutableArray<GitRepository> infos = response.Data;

            if (infos.IsDefaultOrEmpty)
            {
                dispatcherQueue.TryEnqueue(() => StatusMessage = SH.ViewDialogGitSourcesSpeedTestDialogFailedToFetchMirrorListStatusMessage);
                return;
            }

            dispatcherQueue.TryEnqueue(() => StatusMessage = string.Format(SH.ViewDialogGitSourcesSpeedTestDialogTestingMirrorsStatusMessage, infos.Length));

            // Initialize result items for each mirror identifier
            dispatcherQueue.TryEnqueue(() =>
            {
                // 提取唯一的镜像标识符
                HashSet<string> uniqueMirrorIdentifiers = new HashSet<string>(
                    infos.Select(GetFriendlyName),
                    StringComparer.OrdinalIgnoreCase);

                foreach (string mirrorIdentifier in uniqueMirrorIdentifiers)
                {
                    TestResults.Add(new MirrorTestResult
                    {
                        DisplayName = mirrorIdentifier,
                        MirrorKey = mirrorIdentifier
                    });
                }
            });

            if (tester is not null)
            {
                await tester.RunOnceAsync(infos, CancellationToken.None).ConfigureAwait(false);
            }

            // Fetch results from scheduler
            IMirrorScheduler scheduler = scope.ServiceProvider.GetRequiredService<IMirrorScheduler>();

            // 提取唯一的镜像标识符并排序
            List<string> mirrorIdentifiers = new HashSet<string>(
                infos.Select(GetFriendlyName),
                StringComparer.OrdinalIgnoreCase).ToList();
            IReadOnlyList<string> sortedMirrorIdentifiers = scheduler.GetSortedMirrors(mirrorIdentifiers);

            // Update results list on UI thread
            dispatcherQueue.TryEnqueue(() =>
            {
                TestResults.Clear();
                foreach (string mirrorIdentifier in sortedMirrorIdentifiers)
                {
                    MirrorRuntimeStats? stats = scheduler.GetRuntimeStats(mirrorIdentifier);

                    // 找到第一个匹配该标识符的仓库用于显示URL
                    GitRepository? matchingRepo = infos.FirstOrDefault(repo => 
                        string.Equals(GetFriendlyName(repo), mirrorIdentifier, StringComparison.OrdinalIgnoreCase));

                    string displayUrl = matchingRepo?.HttpsUrl.OriginalString ?? string.Empty;

                    TestResults.Add(new MirrorTestResult
                    {
                        Url = displayUrl,
                        DisplayName = mirrorIdentifier,
                        MirrorKey = mirrorIdentifier,
                        Status = SH.ViewDialogGitSourcesSpeedTestDialogCompletedStatusMessage,
                        IsCompleted = true,
                        AverageThroughputMbps = stats?.AvgThroughputMbps ?? 0,
                        AverageConnectMilliseconds = stats?.AvgConnectMs ?? 0,
                        AverageFirstPacketMilliseconds = stats?.AvgFirstPacketMs ?? 0,
                        AverageTtfbMilliseconds = stats?.AvgTTFBMs ?? 0,
                        AverageLsRemoteMilliseconds = stats?.AvgLsRemoteMs ?? 0
                    });
                }

                StatusMessage = SH.ViewDialogGitSourcesSpeedTestDialogAllCompletedStatusMessage;
            });
        }
        catch (Exception ex)
        {
            dispatcherQueue.TryEnqueue(() => StatusMessage = string.Format(SH.ViewDialogGitSourcesSpeedTestDialogErrorStatusMessage, ex.Message));
        }
        finally
        {
            dispatcherQueue.TryEnqueue(() => IsRunning = false);
        }
    }

    /// <summary>
    /// 获取仓库的友好名称或主机名
    /// Get the friendly name or host name of repository
    /// 
    /// 逻辑 / Logic:
    /// 1. 如果有 FriendlyName，则获取本地化字符串
    ///    If has FriendlyName, get localized string
    /// 2. 否则返回 HTTPS URL 的主机名
    ///    Otherwise return host from HTTPS URL
    /// </summary>
    private string GetFriendlyName(GitRepository repository)
    {
        if (!string.IsNullOrWhiteSpace(repository.FriendlyName))
        {
            return SH.GetString("ViewModelSettingNetGitFriendlyName" + repository.FriendlyName);
        }

        return repository.HttpsUrl.Host;
    }

    [Command("SetSelectedMirrorCommand")]
    public async Task SetSelectedMirrorAsync()
    {
        if (SelectedTestResult is null)
        {
            return;
        }

        // 保存选中的源到配置
        // Save selected mirror to configuration
        appOptions.GitRepositoryDomainOverride.Value = SelectedTestResult.MirrorKey;
        appOptions.GitMirrorLastTestTimeUtc.Value = DateTime.UtcNow.ToString("O");

        await taskContext.SwitchToMainThreadAsync();
        StatusMessage = $"已设置为 {SelectedTestResult.DisplayName}";
    }
}

// Use CommunityToolkit MVVM source generator to reduce boilerplate
public partial class MirrorTestResult : ObservableObject
{
    [ObservableProperty]
    public partial string Url { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称（本地化或友好名称）
    /// Display name (localized or friendly name)
    /// </summary>
    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MirrorKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; set; } = SH.ViewDialogGitSourcesSpeedTestDialogPendingStatusMessage;

    /// <summary>
    /// 是否已完成测试
    /// Whether the test is completed.
    /// </summary>
    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    /// <summary>
    /// 平均吞吐量（MB/s）
    /// Average throughput (MB/s).
    /// </summary>
    [ObservableProperty]
    public partial double AverageThroughputMbps { get; set; }

    /// <summary>
    /// 平均连接延迟（毫秒）
    /// Average connect latency (milliseconds).
    /// </summary>
    [ObservableProperty]
    public partial double AverageConnectMilliseconds { get; set; }

    /// <summary>
    /// 平均首包延迟（毫秒）
    /// Average first packet latency (milliseconds).
    /// </summary>
    [ObservableProperty]
    public partial double AverageFirstPacketMilliseconds { get; set; }

    /// <summary>
    /// 平均 TTFB（毫秒）
    /// Average TTFB (milliseconds).
    /// </summary>
    [ObservableProperty]
    public partial double AverageTtfbMilliseconds { get; set; }

    /// <summary>
    /// 平均 ls-remote 延迟（毫秒）
    /// Average ls-remote latency (milliseconds).
    /// </summary>
    [ObservableProperty]
    public partial double AverageLsRemoteMilliseconds { get; set; }

    public string ThroughputDisplay => IsCompleted ? string.Format(CultureInfo.InvariantCulture, "{0:F2} MB/s", AverageThroughputMbps) : string.Empty;

    public string ConnectDisplay => IsCompleted ? string.Format(CultureInfo.InvariantCulture, "Connect {0:F0} ms", AverageConnectMilliseconds) : string.Empty;

    public string FirstPacketDisplay => IsCompleted ? string.Format(CultureInfo.InvariantCulture, "First packet {0:F0} ms", AverageFirstPacketMilliseconds) : string.Empty;

    public string TtfbDisplay => IsCompleted ? string.Format(CultureInfo.InvariantCulture, "TTFB {0:F0} ms", AverageTtfbMilliseconds) : string.Empty;

    public string LsRemoteDisplay => IsCompleted ? string.Format(CultureInfo.InvariantCulture, "ls-remote {0:F0} ms", AverageLsRemoteMilliseconds) : string.Empty;

    // Partial methods invoked by the source generator when properties change.
    partial void OnAverageThroughputMbpsChanged(double value)
    {
        OnPropertyChanged(nameof(ThroughputDisplay));
    }

    partial void OnAverageConnectMillisecondsChanged(double value)
    {
        OnPropertyChanged(nameof(ConnectDisplay));
    }

    partial void OnAverageFirstPacketMillisecondsChanged(double value)
    {
        OnPropertyChanged(nameof(FirstPacketDisplay));
    }

    partial void OnAverageTtfbMillisecondsChanged(double value)
    {
        OnPropertyChanged(nameof(TtfbDisplay));
    }

    partial void OnAverageLsRemoteMillisecondsChanged(double value)
    {
        OnPropertyChanged(nameof(LsRemoteDisplay));
    }

    /// <summary>
    /// 是否已完成测试
    /// Whether the test is completed
    /// </summary>
    partial void OnIsCompletedChanged(bool value)
    {
        OnPropertyChanged(nameof(ThroughputDisplay));
        OnPropertyChanged(nameof(ConnectDisplay));
        OnPropertyChanged(nameof(FirstPacketDisplay));
        OnPropertyChanged(nameof(TtfbDisplay));
        OnPropertyChanged(nameof(LsRemoteDisplay));
    }
}
