// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Task;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

[Service(ServiceLifetime.Singleton, typeof(IPipeRequestHandler))]
public sealed class RemoveTaskHandler : PipeRequestHandler<string>
{
    private readonly IAutomationTaskService automationTaskService;

    public RemoveTaskHandler(IAutomationTaskService automationTaskService)
        : base(PipeRequestKind.RemoveTask)
    {
        this.automationTaskService = automationTaskService;
    }

    protected override ValueTask<PipeResponse> HandleAsync(string id)
    {
        return ValueTask.FromResult<PipeResponse>(automationTaskService.RemoveTask(id));
    }
}
