// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Quartz;
using Snap.Hutao.Remastered.Service.User;
using Snap.Hutao.Remastered.Web.Hoyolab;

namespace Snap.Hutao.Remastered.Service.Job;

[Service(ServiceLifetime.Transient, typeof(IJobScheduler))]
public sealed partial class AutoSignInJobScheduler : IJobScheduler
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    private readonly IUserService userService;

    [GeneratedConstructor]
    public partial AutoSignInJobScheduler(IServiceProvider serviceProvider);

    public async ValueTask ScheduleAsync(IScheduler scheduler)
    {
        TimeSpan serverOffset = await GetCurrentServerOffsetAsync().ConfigureAwait(false);
        DateTimeOffset firstRunTime = GetNextServerDayRandomTime(serverOffset);

        IJobDetail job = JobBuilder.Create<AutoSignInJob>()
            .WithIdentity(JobIdentity.AutoSignInRunJobName, JobIdentity.AutoSignInGroupName)
            .Build();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity(JobIdentity.AutoSignInRunTriggerName, JobIdentity.AutoSignInGroupName)
            .WithSimpleSchedule(builder => builder.WithInterval(Interval).RepeatForever())
            .StartAt(firstRunTime)
            .Build();

        await scheduler.ScheduleJob(job, trigger).ConfigureAwait(false);
    }

    private async ValueTask<TimeSpan> GetCurrentServerOffsetAsync()
    {
        if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is { } userAndUid)
        {
            return PlayerUid.GetRegionTimeZoneUtcOffsetForRegion(userAndUid.Uid.Region);
        }

        return ServerRegionTimeZone.CommonOffset;
    }

    private static DateTimeOffset GetNextServerDayRandomTime(TimeSpan serverOffset)
    {
        DateTimeOffset serverNow = DateTimeOffset.UtcNow.ToOffset(serverOffset);
        DateTimeOffset nextServerMidnight = new(serverNow.Date.AddDays(1), serverOffset);
        int randomSecond = Random.Shared.Next(0, 60);
        return nextServerMidnight.AddSeconds(randomSecond);
    }
}
