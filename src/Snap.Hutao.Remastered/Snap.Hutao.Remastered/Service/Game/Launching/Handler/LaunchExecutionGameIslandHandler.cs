// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Service.Game.Island;
using Snap.Hutao.Remastered.Service.Game.Launching.Context;
using Snap.Hutao.Remastered.Service.Git;
using Snap.Hutao.Remastered.Service.Notification;

namespace Snap.Hutao.Remastered.Service.Game.Launching.Handler;

internal sealed class LaunchExecutionGameIslandHandler : AbstractLaunchExecutionHandler
{
    private readonly bool resume;
    private GameIslandInterop? interop;

    public LaunchExecutionGameIslandHandler(bool resume)
    {
        this.resume = resume;
    }

    public override ValueTask BeforeAsync(BeforeLaunchExecutionContext context)
    {
        if (context.LaunchOptions.IsIslandEnabled.Value)
        {
            interop = new(resume);
            return interop.BeforeAsync(context);
        }

        return ValueTask.CompletedTask;
    }

    public override ValueTask ExecuteAsync(LaunchExecutionContext context)
    {
        if (!context.LaunchOptions.IsIslandEnabled.Value)
        {
            return ValueTask.CompletedTask;
        }

        ExecuteCoreAsync(context).SafeForget();
        return ValueTask.CompletedTask;
    }

    private async ValueTask ExecuteCoreAsync(LaunchExecutionContext context)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(interop);

            await context.TaskContext.SwitchToMainThreadAsync();
            GameLifeCycle.IsIslandConnected.Value = true;

            await context.TaskContext.SwitchToBackgroundAsync();
            await interop.WaitForExitAsync(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            context.Messenger.Send(InfoBarMessage.Error(ex));
            context.Process.Kill();
        }
        finally
        {
            await context.TaskContext.SwitchToMainThreadAsync();
            GameLifeCycle.IsIslandConnected.Value = false;
        }
    }
}