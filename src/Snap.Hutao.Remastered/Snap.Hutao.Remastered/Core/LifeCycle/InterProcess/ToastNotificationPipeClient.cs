// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Model;
using Snap.Hutao.Remastered.Core.Text.Json;
using System.IO.Pipes;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess;

public sealed partial class ToastNotificationPipeClient : IDisposable
{
    private readonly NamedPipeClientStream pipeClient = new(".", PrivateNamedPipe.ToastPipeName, PipeDirection.In);

    public void Dispose()
    {
        pipeClient.Dispose();
    }

    public ToastNotificationRequest? TryGetRequest()
    {
        try
        {
            pipeClient.Connect(100);

            ToastNotificationRequest? request = JsonSerialize.Deserialize<ToastNotificationRequest>(pipeClient, JsonOptions.Default);
            return request;
        }
        catch
        {
            return null;
        }
    }
}
