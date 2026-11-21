// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Model;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess;

/// <summary>
/// Allows an elevated launch to replace a running non-elevated instance before
/// Windows App SDK redirects it back to the existing application.
/// </summary>
internal static class ElevationInstanceHandoff
{
    public static bool TryReplaceUnelevatedInstance(uint processId)
    {
        // Only an actually elevated process may request another instance to exit.
        if (!Environment.IsPrivilegedProcess)
        {
            return false;
        }

        try
        {
            using NamedPipeClientStream pipe = new(".", PrivateNamedPipe.PrivateName, PipeDirection.InOut, PipeOptions.WriteThrough);
            pipe.Connect(2000);
            pipe.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.Request, PipePacketCommand.RequestElevationStatus);
            pipe.Flush();

            pipe.ReadPacket(out PipePacketHeader header, out ElevationStatusResponse? response);
            if (header.Type is not PipePacketType.Response
                || header.Command is not PipePacketCommand.ResponseElevationStatus
                || response is null
                || response.IsElevated
                || response.ProcessId <= 0
                || (uint)response.ProcessId != processId)
            {
                // Never terminate an elevated or unrelated process.
                return false;
            }

            using Process previous = Process.GetProcessById(response.ProcessId);
            pipe.WritePacket(PrivateNamedPipe.PrivateVersion, PipePacketType.SessionTermination, PipePacketCommand.Exit);
            pipe.Flush();

            // The existing instance releases its AppInstance key when it exits.
            // Wait for that release before attempting to become the key owner.
            return previous.WaitForExit(30000);
        }
        catch (ArgumentException)
        {
            // The old process exited between reporting its PID and opening it.
            return true;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException or InvalidOperationException or Win32Exception)
        {
            SentrySdk.CaptureException(ex);
            return false;
        }
    }
}
