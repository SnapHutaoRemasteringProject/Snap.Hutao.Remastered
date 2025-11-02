// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.Security.Principal;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

internal sealed partial class FullTrustNamedPipeDual : IDisposable
{
    private readonly NamedPipeServerStream serverStream = CreatePipeServerStream();
    private readonly NamedPipeClientStream clientStream = new(".", PrivateNamedPipe.FullTrustName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
    private readonly CancellationTokenSource serverTokenSource = new();
    private readonly AsyncLock serverLock = new();

    public void Dispose()
    {
        clientStream.Dispose();

        serverTokenSource.Cancel();
        using AsyncLock.Releaser discard = serverLock.LockAsync().GetAwaiter().GetResult();
        serverTokenSource.Dispose();
        serverStream.Dispose();
    }

    public void Start()
    {
        RunAsync().SafeForget();
    }

    private static NamedPipeServerStream CreatePipeServerStream()
    {
        PipeSecurity pipeSecurity = new();
        pipeSecurity.AddAccessRule(new(SecurityIdentifiers.Everyone, PipeAccessRights.FullControl, AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            PrivateNamedPipe.FullTrustName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough,
            0,
            0,
            pipeSecurity);
    }

    private async ValueTask RunAsync()
    {
        using (await serverLock.LockAsync().ConfigureAwait(false))
        {
            while (!serverTokenSource.IsCancellationRequested)
            {
                try
                {
                    await serverStream.WaitForConnectionAsync(serverTokenSource.Token).ConfigureAwait(false);
                    RunPacketSession(serverStream, serverTokenSource.Token);
                }
                catch (IOException)
                {
                    try
                    {
                        serverStream.Disconnect();
                    }
                    catch
                    {
                        // Ignored
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private void RunPacketSession(NamedPipeServerStream serverStream, CancellationToken token)
    {
        while (serverStream.IsConnected && !token.IsCancellationRequested)
        {
            serverStream.ReadPacket(out FullTrustPipePacketHeader header);
            switch (header.Type, header.Command)
            {
                case (FullTrustPipePacketType.SessionTermination, _):
                    serverStream.Disconnect();
                    return;
            }
        }
    }
}