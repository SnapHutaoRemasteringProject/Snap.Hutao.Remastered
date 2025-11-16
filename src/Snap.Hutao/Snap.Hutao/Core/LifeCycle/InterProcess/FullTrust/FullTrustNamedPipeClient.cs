// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using System.IO.Pipes;

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

internal sealed partial class FullTrustNamedPipeClient : IDisposable
{
    private readonly NamedPipeClientStream clientStream = new(".", PrivateNamedPipe.FullTrustName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
    private readonly Lock syncRoot = new();

    public void Dispose()
    {
        lock (syncRoot)
        {
            clientStream.Dispose();
        }
    }

    public void Create(FullTrustProcessStartInfoRequest request)
    {
        lock (syncRoot)
        {
            EnsureConnected();
            clientStream.WritePacketWithJsonContent(PrivateNamedPipe.FullTrustVersion, FullTrustPipePacketType.Request, FullTrustPipePacketCommand.Create, request);
            clientStream.ReadPacket(out FullTrustPipePacketHeader header);
            HutaoException.ThrowIf(header is not { Type: FullTrustPipePacketType.Response, Command: FullTrustPipePacketCommand.Create }, "Unexpected pipe result");
        }
    }

    public uint StartProcess()
    {
        lock (syncRoot)
        {
            EnsureConnected();
            clientStream.WritePacket(PrivateNamedPipe.FullTrustVersion, FullTrustPipePacketType.Request, FullTrustPipePacketCommand.StartProcess);
            clientStream.ReadPacket(out FullTrustPipePacketHeader header, out FullTrustStartProcessResult? result);
            HutaoException.ThrowIf(header is not { Type: FullTrustPipePacketType.Response, Command: FullTrustPipePacketCommand.StartProcess }, "Unexpected pipe result");

            if (result is null || !result.Succeeded)
            {
                throw HutaoException.Throw($"Failed to start full trust process: [{result?.ErrorMessage}]");
            }

            return result.ProcessId;
        }
    }

    public void LoadLibrary(FullTrustLoadLibraryRequest request)
    {
        lock (syncRoot)
        {
            EnsureConnected();
            clientStream.WritePacketWithJsonContent(PrivateNamedPipe.FullTrustVersion, FullTrustPipePacketType.Request, FullTrustPipePacketCommand.LoadLibrary, request);
            clientStream.ReadPacket(out FullTrustPipePacketHeader header, out FullTrustLoadLibraryResult? result);
            HutaoException.ThrowIf(header is not { Type: FullTrustPipePacketType.Response, Command: FullTrustPipePacketCommand.LoadLibrary }, "Unexpected pipe result");

            if (result is null || !result.Succeeded)
            {
                throw HutaoException.Throw($"Failed to load library on full trust process: [{result?.ErrorMessage}]");
            }
        }
    }

    public void ResumeMainThread()
    {
        lock (syncRoot)
        {
            EnsureConnected();
            clientStream.WritePacket(PrivateNamedPipe.FullTrustVersion, FullTrustPipePacketType.Request, FullTrustPipePacketCommand.ResumeMainThread);
            clientStream.ReadPacket(out FullTrustPipePacketHeader header, out FullTrustResumeMainThreadResult? result);
            HutaoException.ThrowIf(header is not { Type: FullTrustPipePacketType.Response, Command: FullTrustPipePacketCommand.ResumeMainThread }, "Unexpected pipe result");

            if (result is null || !result.Succeeded)
            {
                throw HutaoException.Throw($"Failed to resume main thread: [{result?.ErrorMessage}]");
            }

            clientStream.WritePacket(PrivateNamedPipe.FullTrustVersion, FullTrustPipePacketType.SessionTermination, FullTrustPipePacketCommand.None);
        }
    }

    private void EnsureConnected()
    {
        if (!clientStream.IsConnected)
        {
            clientStream.Connect();
        }
    }
}