// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Game.Launching.Handler;

namespace Snap.Hutao.Remastered.Service.Game.Launching.Invoker;

internal sealed class DefaultLaunchExecutionInvoker : AbstractLaunchExecutionInvoker
{
    public DefaultLaunchExecutionInvoker()
    {
        Handlers =
        [
            new LaunchExecutionGameLifeCycleHandler(resume: false),
            new LaunchExecutionChannelOptionsHandler(),
            new LaunchExecutionGameResourceHandler(convertOnly: false),
            new LaunchExecutionGameIdentityHandler(),
            new LaunchExecutionWindowsHDRHandler(),
            new LaunchExecutionGameProcessStartHandler(),
            new LaunchExecutionGameIslandHandler(resume: false),
            new LaunchExecutionOverlayHandler(),
            new LaunchExecutionStarwardPlayTimeStatisticsHandler(),
            new LaunchExecutionBetterGenshinImpactAutomationHandler()
        ];
    }
}