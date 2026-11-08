// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Service.Game.Launching.Context;
using Snap.Hutao.Remastered.Service.Git;
using Snap.Hutao.Remastered.Win32.Foundation;

namespace Snap.Hutao.Remastered.Service.Game.Launching.Handler;

public sealed class LaunchExecutionGameProcessStartHandler : AbstractLaunchExecutionHandler
{
    public override async ValueTask BeforeAsync(BeforeLaunchExecutionContext context)
    {
        if (context.LaunchOptions.IsIslandEnabled.Value || !HutaoRuntime.IsProcessElevated)
        {
            (bool result, _) = await context.ServiceProvider
                .GetRequiredService<IGitRepositoryService>()
                .EnsureRepositoryAsync("Snap.ContentDelivery")
                .ConfigureAwait(false);

            HutaoException.ThrowIfNot(result, SH.ServiceGameLaunchingHandlerGameProcessStartRepositorySyncFailed);
        }
    }

    public override async ValueTask ExecuteAsync(LaunchExecutionContext context)
    {
        using SentrySpanScope span = SentrySpanScope.StartChild("game.process_start", "Launch game process");
        try
        {
            context.Process.Start();
            span.SetData("game.process_id", context.Process.Id);
            await context.TaskContext.SwitchToMainThreadAsync();
            GameLifeCycle.IsGameRunningProperty.Value = true;
        }
        catch (Win32Exception ex)
        {
            if (ex.HResult is HRESULT.E_FAIL)
            {
                return;
            }

            throw;
        }

        context.Progress.Report(new(SH.ServiceGameLaunchPhaseProcessStarted));
    }
}