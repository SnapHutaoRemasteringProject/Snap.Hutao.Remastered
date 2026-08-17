// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Task;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

[Service(ServiceLifetime.Singleton, typeof(IPipeRequestHandler))]
public sealed class UpdateTaskStepDefinitionHandler : PipeRequestHandler<UpdateAutomationTaskStepDefinition>
{
    private readonly IAutomationTaskService automationTaskService;

    public UpdateTaskStepDefinitionHandler(IAutomationTaskService automationTaskService)
        : base(PipeRequestKind.UpdateTaskStepDefinition)
    {
        this.automationTaskService = automationTaskService;
    }

    protected override ValueTask<PipeResponse> HandleAsync(UpdateAutomationTaskStepDefinition update)
    {
        return ValueTask.FromResult<PipeResponse>(automationTaskService.UpdateTaskStepDefinition(update.Id, update.Index, update.Description));
    }
}
