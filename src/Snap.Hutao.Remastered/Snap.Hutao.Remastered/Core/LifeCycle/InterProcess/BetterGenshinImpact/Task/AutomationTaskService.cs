// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Task;

[Service(ServiceLifetime.Singleton, typeof(IAutomationTaskService))]
public sealed class AutomationTaskService : IAutomationTaskService
{
    public PipeResponse CreateOneShotTask(AutomationTaskDefinition definition)
    {
        return PipeResponse.CreateNone();
    }

    public PipeResponse CreateSteppedTask(SteppedAutomationTaskDefinition definition)
    {
        return PipeResponse.CreateNone();
    }

    public PipeResponse RemoveTask(string id)
    {
        return PipeResponse.CreateNone();
    }

    public PipeResponse UpdateTaskDefinition(string id, string name, string description)
    {
        return PipeResponse.CreateNone();
    }

    public PipeResponse UpdateTaskStepDefinition(string id, int index, string description)
    {
        return PipeResponse.CreateNone();
    }

    public PipeResponse UpdateTaskStepIndex(string id, int index)
    {
        return PipeResponse.CreateNone();
    }

    public PipeResponse AddTaskStepDefinition(string id, string description)
    {
        return PipeResponse.CreateNone();
    }
}