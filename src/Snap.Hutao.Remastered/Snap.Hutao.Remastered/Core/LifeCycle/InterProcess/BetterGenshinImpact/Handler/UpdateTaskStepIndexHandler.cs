// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Task;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

[Service(ServiceLifetime.Singleton, typeof(IPipeRequestHandler))]
public sealed class UpdateTaskStepIndexHandler : PipeRequestHandler<UpdateAutomationTaskStepIndex>
{
    private readonly IAutomationTaskService automationTaskService;

    public UpdateTaskStepIndexHandler(IAutomationTaskService automationTaskService)
        : base(PipeRequestKind.UpdateTaskStepIndex)
    {
        this.automationTaskService = automationTaskService;
    }

    protected override ValueTask<PipeResponse> HandleAsync(UpdateAutomationTaskStepIndex update)
    {
        return ValueTask.FromResult<PipeResponse>(automationTaskService.UpdateTaskStepIndex(update.Id, update.Index));
    }
}
