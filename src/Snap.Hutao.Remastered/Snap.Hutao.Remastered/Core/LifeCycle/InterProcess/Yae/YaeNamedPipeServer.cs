// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Diagnostics;
using Snap.Hutao.Remastered.Service.Game;
using Snap.Hutao.Remastered.Service.Yae.Achievement;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Yae;

public sealed class YaeNamedPipeServer : IAsyncDisposable
{
    private const string PipeName = "YaeAchievementPipe";

    private readonly CancellationTokenSource disposeCts = new();
    private readonly AsyncLock disposeLock = new();

    private readonly TargetNativeConfiguration config;
    private readonly YaeDataRequest request;
    private readonly ITaskContext taskContext;
    private readonly IProcess gameProcess;

    private readonly NamedPipeServerStream serverStream;

    private volatile bool disposed;

    public YaeNamedPipeServer(IServiceProvider serviceProvider, IProcess gameProcess, TargetNativeConfiguration config, YaeDataRequest request)
    {
        Verify.Operation(HutaoRuntime.IsProcessElevated, "Snap Hutao must be elevated to use Yae.");

        taskContext = serviceProvider.GetRequiredService<ITaskContext>();

        this.gameProcess = gameProcess;
        this.config = config;
        this.request = request;

        // Yae is always running elevated, so we don't need to use ACL method.
        serverStream = new(PipeName);
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        await disposeCts.CancelAsync().ConfigureAwait(false);
        using (await disposeLock.LockAsync().ConfigureAwait(false))
        {
            // Discard
        }

        disposeCts.Dispose();
        await serverStream.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask<ImmutableArray<YaeData>> GetDataArrayAsync(CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(disposed, "YaeNamedPipeServer is disposed.");

        using (await disposeLock.LockAsync().ConfigureAwait(false))
        {
            while (await GameLifeCycle.IsGameRunningAsync(taskContext).ConfigureAwait(false))
            {
                try
                {
                    using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(disposeCts.Token, token))
                    {
                        await serverStream.WaitForConnectionAsync(cts.Token).ConfigureAwait(false);
                        return GetDataArray(serverStream);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        return default;
    }

    private unsafe YaeData? HandleCommand(BinaryReader reader, BinaryWriter writer, TargetNativeConfiguration config)
    {
        YaeCommandKind kind = reader.Read<YaeCommandKind>();

        switch (kind)
        {
            case YaeCommandKind.RequestPacketList:
                {
                    foreach (uint cmdId in request.PacketCmdIds)
                    {
                        writer.Write(cmdId);
                    }

                    writer.Write(uint.MaxValue);
                    writer.Flush();
                    return default;
                }

            case YaeCommandKind.RequestPlayerPropList:
                {
                    foreach (InterestedPropType propType in request.PlayerPropTypes)
                    {
                        writer.Write((uint)propType);
                    }

                    writer.Write(uint.MaxValue);
                    writer.Flush();
                    return default;
                }

            case YaeCommandKind.RequestRva:
                {
                    writer.Write(config.DoCmd);
                    writer.Write(config.UpdateNormalProperty);
                    writer.Write(config.NewString);
                    writer.Write(config.FindGameObject);
                    writer.Write(config.EventSystemUpdate);
                    writer.Write(config.SimulatePointerClick);
                    writer.Write(config.ToInt32);
                    writer.Write(config.TcpStatePtr);
                    writer.Write(config.SharedInfoPtr);
                    writer.Write(config.Decompress);
                    return default;
                }

            case YaeCommandKind.RequestResumeThread:
                {
                    gameProcess.ResumeMainThread();
                    return default;
                }

            case YaeCommandKind.ResponsePacket:
                {
                    uint cmdId = reader.Read<ushort>();
                    int contentLength = reader.Read<int>();
                    IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.RentExactly(contentLength);
                    reader.ReadExactly(owner.Memory.Span);

                    // true 表示后续不再需要该数据包
                    writer.Write(true);
                    return new(kind, owner, contentLength, cmdId);
                }

            case YaeCommandKind.ResponsePlayerProp:
                {
                    int contentLength = sizeof(YaePropertyTypeValue);
                    IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.RentExactly(contentLength);
                    reader.ReadExactly(owner.Memory.Span);

                    // true 表示后续不再需要该属性
                    writer.Write(true);
                    return new(kind, owner, contentLength);
                }

            case YaeCommandKind.SessionEnd:
                {
                    writer.Write(true);
                    writer.Flush();
                    return YaeData.SessionEnd;
                }

            default:
                return default;
        }
    }

    private ImmutableArray<YaeData> GetDataArray(NamedPipeServerStream serverStream)
    {
        Debug.Assert(Thread.CurrentThread.IsBackground);

        // This method is never cancellable.
        // Yae will prompt error message if the server is closed.
        using BinaryReader reader = new(serverStream, Encoding.UTF8);
        using BinaryWriter writer = new(serverStream, Encoding.UTF8);

        ImmutableArray<YaeData>.Builder builder = ImmutableArray.CreateBuilder<YaeData>();
        while (gameProcess.IsRunning && serverStream.IsConnected)
        {
            try
            {
                // If command is SessionEnd, pipe client will close connection after HandleCommand.
                // Actually, yae exit after read the final bool value.
                if (HandleCommand(reader, writer, config) is { } data)
                {
                    if (data.Kind is YaeCommandKind.SessionEnd)
                    {
                        gameProcess.Kill();
                    }
                    else
                    {
                        builder.Add(data);
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // Client closed.
            }
            catch (IOException)
            {
                // Pipe is broken.
            }
        }

        return builder.ToImmutable();
    }
}