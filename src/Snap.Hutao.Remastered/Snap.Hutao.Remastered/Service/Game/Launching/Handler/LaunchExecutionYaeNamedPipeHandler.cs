// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Yae;
using Snap.Hutao.Remastered.Service.Game.Island;
using Snap.Hutao.Remastered.Service.Game.Launching.Context;
using Snap.Hutao.Remastered.Service.Yae.Achievement;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Game.Launching.Handler;

internal sealed class LaunchExecutionYaeNamedPipeHandler : AbstractLaunchExecutionHandler
{
    private readonly TargetNativeConfiguration config;
    private readonly YaeDataArrayReceiver receiver;

    public LaunchExecutionYaeNamedPipeHandler(TargetNativeConfiguration config, YaeDataArrayReceiver receiver)
    {
        this.config = config;
        this.receiver = receiver;
    }

    public override async ValueTask ExecuteAsync(LaunchExecutionContext context)
    {
        if (!HutaoRuntime.IsProcessElevated)
        {
            context.Process.Kill();
            HutaoException.NotSupported(SH.ServiceGameLaunchingHandlerEmbeddedYaeClientNotElevated);
        }

        if (!context.LaunchOptions.IsIslandEnabled.Value)
        {
            context.Process.Kill();
            HutaoException.NotSupported(SH.ServiceGameLaunchingHandlerEmbeddedYaeIslandNotEnabled);
            return;
        }

        string dataFolderYaePath = Path.Combine(HutaoRuntime.DataDirectory, "YaeAchievementLib.dll");
        InstalledLocation.CopyFileFromApplicationUri("ms-appx:///YaeAchievementLib.dll", dataFolderYaePath);

        try
        {
            DllInjectionUtilities.InjectUsingRemoteThread(dataFolderYaePath, "YaeMain", context.Process.Id);
        }
        catch (Exception ex)
        {
            // Windows Defender Application Control
            if (HutaoNative.IsWin32(ex.HResult, WIN32_ERROR.ERROR_SYSTEM_INTEGRITY_POLICY_VIOLATION))
            {
                context.Process.Kill();
                throw HutaoException.Throw(SH.ServiceGameLaunchingHandlerEmbeddedYaeErrorSystemIntegrityPolicyViolation);
            }

            throw;
        }

        try
        {
#pragma warning disable CA2007
            await using (YaeNamedPipeServer server = new(context.ServiceProvider, context.Process, config))
#pragma warning restore CA2007
            {
                receiver.Array = await server.GetDataArrayAsync().ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            context.Process.Kill();
            throw;
        }
    }
}