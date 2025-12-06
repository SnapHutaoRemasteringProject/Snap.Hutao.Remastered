// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Yae;
using Snap.Hutao.Remastered.Service.Game.Launching.Context;
using Snap.Hutao.Remastered.Service.Game.Launching.Handler;
using Snap.Hutao.Remastered.Service.Yae.Achievement;

namespace Snap.Hutao.Remastered.Service.Game.Launching.Invoker;

internal sealed class YaeLaunchExecutionInvoker : AbstractLaunchExecutionInvoker
{
    public YaeLaunchExecutionInvoker(TargetNativeConfiguration config, YaeDataArrayReceiver receiver)
    {
        Handlers =
        [
            new LaunchExecutionGameLifeCycleHandler(resume: false),
            new LaunchExecutionChannelOptionsHandler(),
            new LaunchExecutionGameResourceHandler(false),
            new LaunchExecutionGameIdentityHandler(),
            new LaunchExecutionGameProcessStartHandler(),
            new LaunchExecutionYaeNamedPipeHandler(config, receiver),
        ];
    }

    protected override IProcess? CreateProcess(BeforeLaunchExecutionContext beforeContext)
    {
        return GameProcessFactory.CreateForEmbeddedYae(beforeContext);
    }
}