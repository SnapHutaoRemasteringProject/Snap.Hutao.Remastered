// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Model;
using Snap.Hutao.Remastered.Core.Security.Principal;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess;

[Service(ServiceLifetime.Singleton)]
public sealed partial class PrivateNamedPipeServer : IDisposable
{
    private readonly BetterGenshinImpactNamedPipeServer betterGenshinImpactNamedPipeServer;
    private readonly PrivateNamedPipeMessageDispatcher messageDispatcher;
    private readonly ILogger<PrivateNamedPipeServer> logger;

    private readonly NamedPipeServerStream serverStream = CreatePipeServerStream();
    private readonly CancellationTokenSource serverTokenSource = new();
    private readonly AsyncLock serverLock = new();

    [GeneratedConstructor]
    public partial PrivateNamedPipeServer(IServiceProvider serviceProvider);

    public void Dispose()
    {
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
            PrivateNamedPipe.PrivateName,
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
                    logger.LogInformation("Pipe session created");
                    await RunPacketSession(serverStream, serverTokenSource.Token).ConfigureAwait(false);
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

    private async ValueTask RunPacketSession(NamedPipeServerStream serverStream, CancellationToken token)
    {
        while (serverStream.IsConnected && !token.IsCancellationRequested)
        {
            serverStream.ReadPacket(out PipePacketHeader header);
            switch (header.Type, header.Command)
            {
                case (PipePacketType.Request, PipePacketCommand.RequestElevationStatus):
                    ElevationStatusResponse resp = new(HutaoRuntime.IsProcessElevated, Environment.ProcessId);
                    serverStream.WritePacketWithJsonContent(PrivateNamedPipe.PrivateVersion, PipePacketType.Response, PipePacketCommand.ResponseElevationStatus, resp);
                    serverStream.Flush();
                    break;

                case (PipePacketType.Request, PipePacketCommand.RedirectActivation):
                    HutaoActivationArguments? hutaoArgs = serverStream.ReadJsonContent<HutaoActivationArguments>(in header);
                    messageDispatcher.RedirectedActivation(hutaoArgs);
                    break;

                case (PipePacketType.Request, PipePacketCommand.BetterGenshinImpactToSnapHutaoRequest):
                    PipeRequest<JsonElement>? request = serverStream.ReadJsonContent<PipeRequest<JsonElement>>(in header);
                    PipeResponse response = await betterGenshinImpactNamedPipeServer.DispatchRequest(request).ConfigureAwait(false);
                    serverStream.WritePacketWithJsonContent(PrivateNamedPipe.PrivateVersion, PipePacketType.Response, PipePacketCommand.SnapHutaoToBetterGenshinImpactResponse, response);
                    serverStream.Flush();
                    break;

                case (PipePacketType.SessionTermination, _):
                    serverStream.Disconnect();
                    if (header.Command is PipePacketCommand.Exit)
                    {
                        messageDispatcher.ExitApplication();
                    }

                    return;
            }
        }
    }
}