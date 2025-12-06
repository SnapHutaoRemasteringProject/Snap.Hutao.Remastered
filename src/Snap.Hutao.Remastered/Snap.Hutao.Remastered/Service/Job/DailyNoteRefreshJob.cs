// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Quartz;
using Snap.Hutao.Remastered.Service.DailyNote;

namespace Snap.Hutao.Remastered.Service.Job;

internal sealed partial class DailyNoteRefreshJob : IJob
{
    private readonly IDailyNoteService dailyNoteService;

    [GeneratedConstructor]
    public partial DailyNoteRefreshJob(IServiceProvider serviceProvider);

    [SuppressMessage("", "SH003")]
    public async Task Execute(IJobExecutionContext context)
    {
        await dailyNoteService.RefreshDailyNotesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}