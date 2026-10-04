// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Quartz;

namespace Snap.Hutao.Remastered.Service.Job;

public sealed partial class AutoSignInJob : IJob
{
    private readonly IAutoSignInService autoSignInService;
    private readonly ILogger<AutoSignInJob> logger;

    [GeneratedConstructor]
    public partial AutoSignInJob(IServiceProvider serviceProvider);

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("AutoSignInJob triggered at {NowUtc:O}", DateTimeOffset.UtcNow);

        // Every saved account must be signed in, not only the current one.
        await autoSignInService.RunForAllUsersAsync(cancellationToken).ConfigureAwait(false);
    }
}
