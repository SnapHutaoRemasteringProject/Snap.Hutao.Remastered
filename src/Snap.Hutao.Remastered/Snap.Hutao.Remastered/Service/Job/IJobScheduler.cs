// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Quartz;

namespace Snap.Hutao.Remastered.Service.Job;

internal interface IJobScheduler
{
    ValueTask ScheduleAsync(IScheduler scheduler);
}
