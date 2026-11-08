// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Model;
using Snap.Hutao.Remastered.Core.Text.Json;
using System.IO.Pipes;

namespace Snap.Hutao.Remastered.Service.Notification;

public sealed partial class ToastNotificationPipeServer : IDisposable
{
    private readonly NamedPipeServerStream pipeServer = new(PrivateNamedPipe.ToastPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte);
    private bool isDisposed;

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            pipeServer.Dispose();
        }
    }

    public bool TrySendRequest(ToastNotificationRequest request)
    {
        try
        {
            // Wait for helper to connect (5s timeout)
            if (!Task.Run(pipeServer.WaitForConnection).Wait(TimeSpan.FromSeconds(5)))
            {
                return false;
            }

            JsonSerialize.Serialize(pipeServer, request, JsonOptions.Default);
            pipeServer.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
