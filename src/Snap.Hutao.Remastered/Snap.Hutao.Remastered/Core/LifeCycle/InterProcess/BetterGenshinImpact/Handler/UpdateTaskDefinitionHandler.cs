// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Task;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Handler;

[Service(ServiceLifetime.Singleton, typeof(IPipeRequestHandler))]
public sealed class UpdateTaskDefinitionHandler : PipeRequestHandler<AutomationTaskDefinition>
{
    private readonly IAutomationTaskService automationTaskService;

    public UpdateTaskDefinitionHandler(IAutomationTaskService automationTaskService)
        : base(PipeRequestKind.UpdateTaskDefinition)
    {
        this.automationTaskService = automationTaskService;
    }

    protected override ValueTask<PipeResponse> HandleAsync(AutomationTaskDefinition update)
    {
        return ValueTask.FromResult<PipeResponse>(automationTaskService.UpdateTaskDefinition(update.Id, update.Name, update.Description));
    }
}
