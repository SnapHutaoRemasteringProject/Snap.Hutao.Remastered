// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Property;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service.Abstraction;
using Snap.Hutao.Remastered.Service.Job;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.DailyNote;

[Service(ServiceLifetime.Singleton)]
public sealed partial class DailyNoteOptions : DbStoreOptions
{
    private const int OneMinute = 60;

    private readonly IQuartzService quartzService;
    private readonly ITileRefreshScheduler tileRefreshScheduler;

    [GeneratedConstructor(CallBaseConstructor = true)]
    public partial DailyNoteOptions(IServiceProvider serviceProvider);

    public ImmutableArray<NameValue<int>> RefreshTimes { get; } =
    [
        new(SH.ViewModelDailyNoteRefreshTime4, OneMinute * 4),
        new(SH.ViewModelDailyNoteRefreshTime8, OneMinute * 8),
        new(SH.ViewModelDailyNoteRefreshTime30, OneMinute * 30),
        new(SH.ViewModelDailyNoteRefreshTime40, OneMinute * 40),
        new(SH.ViewModelDailyNoteRefreshTime60, OneMinute * 60),
    ];

    [field: MaybeNull]
    public IObservableProperty<bool> IsAutoRefreshEnabled { get => field ??= CreateProperty(SettingKeys.DailyNoteIsAutoRefreshEnabled, false).WithValueChangedCallback(OnIsAutoRefreshEnabledChanged, this); }

    [field: MaybeNull]
    public IObservableProperty<NameValue<int>?> SelectedRefreshTime { get => field ??= CreateProperty(SettingKeys.DailyNoteRefreshSeconds, OneMinute * 30).WithValueChangedCallback(OnSelectedRefreshTimeChanged, this).AsNameValue(RefreshTimes); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsReminderNotification { get => field ??= CreateProperty(SettingKeys.DailyNoteReminderNotify, false); }

    [field: MaybeNull]
    public IObservableProperty<bool> IsSilentWhenPlayingGame { get => field ??= CreateProperty(SettingKeys.DailyNoteSilentWhenPlayingGame, false); }

    [field: MaybeNull]
    public IObservableProperty<string?> WebhookUrl { get => field ??= CreateProperty(SettingKeys.DailyNoteWebhookUrl); }

    [field: MaybeNull]
    public IObservableProperty<string?> TileBackgroundAvatarId { get => field ??= CreateProperty(SettingKeys.DailyNoteTileBackgroundAvatarId); }

    [field: MaybeNull]
    public IObservableProperty<string?> TileBackgroundUid { get => field ??= CreateProperty(SettingKeys.DailyNoteTileBackgroundUid); }

    private static void OnIsAutoRefreshEnabledChanged(bool value, DailyNoteOptions options)
    {
        if (value)
        {
            if (options.SelectedRefreshTime.Value is { } refreshTime)
            {
                int intervalSeconds = refreshTime.Value;

                // 开启自动刷新时「确保」Job 已注册：启动时开关若是关的，调度器不会注册 Job，
                // 此时单纯的 UpdateJobAsync 找不到 trigger 会静默无效。EnsureJobAsync 在
                // trigger 不存在时首次创建，存在时重建触发器以应用最新间隔。
                options.quartzService.EnsureJobAsync(
                    JobIdentity.DailyNoteGroupName,
                    JobIdentity.DailyNoteRefreshTriggerName,
                    DailyNoteRefreshJobScheduler.BuildJob,
                    jobKey => DailyNoteRefreshJobScheduler.BuildTrigger(jobKey, intervalSeconds)).GetAwaiter().GetResult();
            }
        }
        else
        {
            options.quartzService.StopJobAsync(JobIdentity.DailyNoteGroupName, JobIdentity.DailyNoteRefreshTriggerName).GetAwaiter().GetResult();
        }
    }

    private static void OnSelectedRefreshTimeChanged(int value, DailyNoteOptions options)
    {
        // 间隔变化只影响「已启用自动刷新」的场景；开关关着时无需调度。
        // 用 EnsureJobAsync 而非 UpdateJobAsync：切换间隔时 Job 若因开关关闭而从未注册，
        // UpdateJobAsync 找不到 trigger 会静默无效；EnsureJobAsync 会按需创建。
        // 必须异步派发：本回调跑在 UI 线程上，而注册路径要排队等启动阶段的
        // 配置任务（它可能正在等 UI 线程），同步等待会互相卡死。
        if (options.IsAutoRefreshEnabled.Value)
        {
            options.quartzService.EnsureJobAsync(
                JobIdentity.DailyNoteGroupName,
                JobIdentity.DailyNoteRefreshTriggerName,
                DailyNoteRefreshJobScheduler.BuildJob,
                jobKey => DailyNoteRefreshJobScheduler.BuildTrigger(jobKey, value)).SafeForget();
        }

        // 计划任务的间隔也要跟着设置变，否则后台刷新还停留在旧频率。
        // SyncIntervalAsync 内部会跳过未注册状态，不会悄悄把开关打开。
        // 接 ShutdownToken：应用退出时中止挂起的任务重建。
        options.tileRefreshScheduler.SyncIntervalAsync(Math.Max(1, value / 60), XamlApplicationLifetime.ShutdownToken).AsTask().SafeForget();
    }
}