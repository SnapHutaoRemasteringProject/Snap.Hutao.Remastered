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
        using SentrySpanScope span = SentrySpanScope.StartOperation("pipe.redirect_activation", "app.activation");
        span.SetData("activation.kind", hutaoArgs.Kind.ToString());

        if (!clientStream.TryConnectOnce())
        {
            span.SetData("pipe.connected", false);
            return false;
        }

        span.SetData("pipe.connected", true);

        try
        {
            clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.Request, PipePacketCommand.RequestElevationStatus);
            clientStream.ReadPacket(out PipePacketHeader _, out ElevationStatusResponse? response);
            ArgumentNullException.ThrowIfNull(response);
            span.SetData("pipe.peer_elevated", response.IsElevated);

            // Prefer elevated instance
            if (HutaoRuntime.IsProcessElevated && !response.IsElevated)
            {
                // Notify previous instance to exit
                clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.SessionTermination, PipePacketCommand.Exit);
                clientStream.Flush();
                WaitPreviousProcessExit(response);

                // Retain the elevated instance
                span.SetData("pipe.previous_instance_terminated", true);
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
            span.SetData("pipe.broken", true);
            return false;
        }
    }

    /// <summary>
    /// Lightweight one-shot redirect from the second instance to the first,
    /// without needing DI or the singleton pipe client. Called from <see cref="Bootstrap.Main"/>.
    /// </summary>
    /// <param name="args">The command-line arguments from Main().</param>
    /// <returns>true if a redirect was sent; false if pipe unavailable.</returns>
    public static bool TryLightweightRedirect(string[] args)
    {
        HutaoActivationArguments hutaoArgs;

        // Determine activation kind from command-line arguments
        if (args.Length > 0)
        {
            string? arg = args[0];
            if (arg is not null && arg.StartsWith("hutao://", StringComparison.OrdinalIgnoreCase))
            {
                hutaoArgs = new()
                {
                    Kind = HutaoActivationKind.Protocol,
                    ProtocolActivatedUri = new Uri(arg),
                    IsRedirectTo = true,
                };
            }
            else
            {
                // Regular launch with arguments
                hutaoArgs = new()
                {
                    Kind = HutaoActivationKind.Launch,
                    LaunchActivatedArguments = arg,
                    IsRedirectTo = true,
                };
            }
        }
        else
        {
            // No arguments, treat as a regular launch to bring existing instance to foreground
            hutaoArgs = new()
            {
                Kind = HutaoActivationKind.Launch,
                LaunchActivatedArguments = string.Empty,
                IsRedirectTo = true,
            };
        }

        using NamedPipeClientStream clientStream = new(".", PrivateNamedPipe.PrivateName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

        using (SentrySpanScope connect = SentrySpanScope.StartChild("pipe.connect", PrivateNamedPipe.PrivateName))
        {
            connect.SetData("activation.kind", hutaoArgs.Kind.ToString());

            try
            {
                clientStream.Connect(TimeSpan.Zero);
                connect.SetData("pipe.connected", true);
            }
            catch (TimeoutException)
            {
                connect.SetData("pipe.connected", false);
                return false;
            }
        }

        using (SentrySpanScope span = SentrySpanScope.StartChild("pipe.redirect", "RequestElevationStatus + RedirectActivation"))
        {
            try
            {
                clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.Request, PipePacketCommand.RequestElevationStatus);
                clientStream.ReadPacket(out PipePacketHeader _, out ElevationStatusResponse? response);
                ArgumentNullException.ThrowIfNull(response);
                span.SetData("pipe.peer_elevated", response.IsElevated);

                if (HutaoRuntime.IsProcessElevated && !response.IsElevated)
                {
                    clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.SessionTermination, PipePacketCommand.Exit);
                    clientStream.Flush();
                    span.SetData("pipe.previous_instance_terminated", true);
                    return false;
                }

                clientStream.WritePacketWithJsonContent(PrivateNamedPipe.PrivateVersion, PipePacketType.Request, PipePacketCommand.RedirectActivation, hutaoArgs);
                clientStream.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.SessionTermination, PipePacketCommand.None);
                clientStream.Flush();

                return true;
            }
            catch (IOException)
            {
                span.SetData("pipe.broken", true);
                return false;
            }
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