// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Windows.AppLifecycle;
using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Model;
using Snap.Hutao.Remastered.Factory.Process;
using System.IO;
using System.IO.Pipes;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess;

[Service(ServiceLifetime.Singleton)]
public sealed partial class PrivateNamedPipeClient : IDisposable
{
    private readonly NamedPipeClientStream clientStream = new(".", PrivateNamedPipe.PrivateName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

    public bool TryRedirectActivationTo(AppActivationArguments args)
    {
        return TryRedirectActivationTo(HutaoActivationArguments.FromAppActivationArguments(args, isRedirected: true));
    }

    public bool TryRedirectActivationTo(HutaoActivationArguments hutaoArgs)
    {
        if (!clientStream.TryConnectOnce())
        {
            return false;
        }

        try
        {
            clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.Request, PipePacketCommand.RequestElevationStatus);
            clientStream.ReadPacket(out PipePacketHeader _, out ElevationStatusResponse? response);
            ArgumentNullException.ThrowIfNull(response);

            // Prefer elevated instance
            if (HutaoRuntime.IsProcessElevated && !response.IsElevated)
            {
                // Notify previous instance to exit
                clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.SessionTermination, PipePacketCommand.Exit);
                clientStream.Flush();
                WaitPreviousProcessExit(response);

                // Retain the elevated instance
                return false;
            }

            // Redirect to previous instance
            clientStream.WritePacketWithJsonContent(PrivateNamedPipe.PrivateVersion, PipePacketType.Request, PipePacketCommand.RedirectActivation, hutaoArgs);
            clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.SessionTermination, PipePacketCommand.None);
            clientStream.Flush();

            return true;
        }
        catch (IOException)
        {
            // Pipe is broken.
            return false;
        }
    }

    /// <summary>
    /// Lightweight one-shot redirect from an unpackaged second instance to the first,
    /// without needing DI or the singleton pipe client. Called from <see cref="Bootstrap.Main"/>.
    /// </summary>
    /// <param name="args">The command-line arguments from Main().</param>
    /// <returns>true if a redirect was sent; false if packaged or the pipe is unavailable.</returns>
    public static bool TryLightweightRedirect(string[] args)
    {
        // Packaged activation may need the notification COM server. Defer to OnLaunched,
        // which subscribes and registers before reading and redirecting the activation.
        if (RuntimeEnvironment.IsPackaged)
        {
            return false;
        }

        HutaoActivationArguments hutaoArgs = HutaoActivationArguments.TryCreateFromCommandLineArguments(args)
            ?? new()
            {
                // No arguments, treat as a regular launch to bring existing instance to foreground
                Kind = HutaoActivationKind.Launch,
                LaunchActivatedArguments = string.Empty,
            };

        hutaoArgs.IsRedirectTo = true;

        using NamedPipeClientStream clientStream = new(".", PrivateNamedPipe.PrivateName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

        try
        {
            clientStream.Connect(TimeSpan.Zero);
        }
        catch (TimeoutException)
        {
            return false;
        }

        try
        {
            clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.Request, PipePacketCommand.RequestElevationStatus);
            clientStream.ReadPacket(out PipePacketHeader _, out ElevationStatusResponse? response);
            ArgumentNullException.ThrowIfNull(response);

            if (HutaoRuntime.IsProcessElevated && !response.IsElevated)
            {
                clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.SessionTermination, PipePacketCommand.Exit);
                clientStream.Flush();
                return false;
            }

            clientStream.WritePacketWithJsonContent(PrivateNamedPipe.PrivateVersion, PipePacketType.Request, PipePacketCommand.RedirectActivation, hutaoArgs);
            clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.SessionTermination, PipePacketCommand.None);
            clientStream.Flush();

            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        clientStream.Dispose();
    }

    private static void WaitPreviousProcessExit(ElevationStatusResponse response)
    {
        if (!ProcessFactory.TryGetById(response.ProcessId, out IProcess? process))
        {
            return;
        }

        if (process is { HasExited: false })
        {
            process.SafeWaitForExit();
        }

        SpinWaitPolyfill.SpinUntil(response, static response => !ProcessFactory.TryGetById(response.ProcessId, out _));
    }
}
