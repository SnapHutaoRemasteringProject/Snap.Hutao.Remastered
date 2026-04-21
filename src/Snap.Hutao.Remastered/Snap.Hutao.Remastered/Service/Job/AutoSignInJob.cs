// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Quartz;
using Snap.Hutao.Remastered.Service.User;

namespace Snap.Hutao.Remastered.Service.Job;

public sealed partial class AutoSignInJob : IJob
{
    private readonly IUserService userService;
    private readonly IAutoSignInService autoSignInService;
    private readonly ILogger<AutoSignInJob> logger;

    [GeneratedConstructor]
    public partial AutoSignInJob(IServiceProvider serviceProvider);

    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("AutoSignInJob triggered at {NowUtc:O}", DateTimeOffset.UtcNow);

        if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is not { } userAndUid)
        {
            logger.LogInformation("AutoSignInJob skipped: no current user/uid");
            return;
        }

        logger.LogInformation("AutoSignInJob running for uid {Uid}", userAndUid.Uid.Value);
        await autoSignInService.RunOnceAsync(userAndUid, context.CancellationToken).ConfigureAwait(false);
    }
}
