// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Task;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

[Service(ServiceLifetime.Singleton, typeof(IPipeRequestHandler))]
public sealed class CreateSteppedTaskHandler : PipeRequestHandler<SteppedAutomationTaskDefinition>
{
    private readonly IAutomationTaskService automationTaskService;

    public CreateSteppedTaskHandler(IAutomationTaskService automationTaskService)
        : base(PipeRequestKind.CreateSteppedTask)
    {
        this.automationTaskService = automationTaskService;
    }

    protected override ValueTask<PipeResponse> HandleAsync(SteppedAutomationTaskDefinition definition)
    {
        return ValueTask.FromResult<PipeResponse>(automationTaskService.CreateSteppedTask(definition));
    }
}
