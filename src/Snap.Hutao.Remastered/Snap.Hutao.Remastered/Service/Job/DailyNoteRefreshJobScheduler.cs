// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Quartz;
using Snap.Hutao.Remastered.Service.DailyNote;

namespace Snap.Hutao.Remastered.Service.Job;

[Service(ServiceLifetime.Transient, typeof(IJobScheduler))]
public sealed partial class DailyNoteRefreshJobScheduler : IJobScheduler
{
    private readonly DailyNoteOptions dailyNoteOptions;

    [GeneratedConstructor]
    public partial DailyNoteRefreshJobScheduler(IServiceProvider serviceProvider);

    public async ValueTask ScheduleAsync(IScheduler scheduler, CancellationToken token = default)
    {
        if (!TryGetRefreshInterval(out int interval))
        {
            return;
        }

        IJobDetail job = BuildJob();
        await scheduler.ScheduleJob(job, BuildTrigger(job.Key, interval), cancellationToken: token).ConfigureAwait(false);
    }

    /// <summary>
    /// 构造每日便笺刷新 Job。抽出为静态工厂，供启动注册与运行中「确保存在」复用，
    /// 避免两处各写一份 JobDetail/Trigger 构造。
    /// </summary>
    public static IJobDetail BuildJob()
    {
        return JobBuilder.Create<DailyNoteRefreshJob>()
            .WithIdentity(JobIdentity.DailyNoteRefreshJobName, JobIdentity.DailyNoteGroupName)
            .Build();
    }

    /// <summary>
    /// 构造每日便笺刷新 Trigger：按 <paramref name="intervalSeconds"/> 周期重复，首次在间隔后触发，
    /// 并挂到 <paramref name="jobKey"/> 上。
    /// </summary>
    public static ITrigger BuildTrigger(JobKey jobKey, int intervalSeconds)
    {
        return TriggerBuilder.Create()
            .WithIdentity(JobIdentity.DailyNoteRefreshTriggerName, JobIdentity.DailyNoteGroupName)
            .ForJob(jobKey)
            .WithSimpleSchedule(builder => builder.WithInterval(TimeSpan.FromSeconds(intervalSeconds)).RepeatForever())
            .StartAt(DateTimeOffset.Now.AddSeconds(intervalSeconds))
            .Build();
    }

    private bool TryGetRefreshInterval(out int interval)
    {
        if (dailyNoteOptions is { IsAutoRefreshEnabled.Value: true, SelectedRefreshTime.Value: { } refreshTime })
        {
            interval = refreshTime.Value;
            return true;
        }

        interval = 0;
        return false;
    }
}
