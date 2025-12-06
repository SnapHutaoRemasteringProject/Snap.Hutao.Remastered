// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Service.Game.Launching.Context;
using Snap.Hutao.Remastered.Service.Game.Launching.Handler;

namespace Snap.Hutao.Remastered.Service.Game.Launching.Invoker;

internal sealed class ResumeLaunchExecutionInvoker : AbstractLaunchExecutionInvoker
{
    public ResumeLaunchExecutionInvoker()
    {
        Handlers =
        [
            new LaunchExecutionGameLifeCycleHandler(resume: true),
            new LaunchExecutionGameIslandHandler(resume: true),
            new LaunchExecutionOverlayHandler()
        ];
    }

    protected override IProcess? CreateProcess(BeforeLaunchExecutionContext context)
    {
        context.TryGetOption(LaunchExecutionOptionsKey.RunningProcess, out IProcess? process);
        return process;
    }
}