// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using System.Buffers;
using System.IO.Hashing;
using System.IO.Pipes;

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

internal static class PipeStreamExtension
{
    public static TData? ReadJsonContent<TData>(this PipeStream stream, ref readonly FullTrustPipePacketHeader header)
    {
        using (IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.RentExactly(header.ContentLength))
        {
            Span<byte> content = memoryOwner.Memory.Span;
            stream.ReadExactly(content);
            HutaoException.ThrowIf(XxHash64.HashToUInt64(content) != header.Checksum, "PipePacket Content Hash incorrect");

            return JsonSerializer.Deserialize<TData>(content);
        }
    }

    public static void ReadPacket<TData>(this PipeStream stream, out FullTrustPipePacketHeader header, out TData? data)
        where TData : class
    {
        data = default;

        stream.ReadPacket(out header);
        if (header.ContentType is FullTrustPipePacketContentType.Json)
        {
            data = stream.ReadJsonContent<TData>(in header);
        }
    }

    public static unsafe void ReadPacket(this PipeStream stream, out FullTrustPipePacketHeader header)
    {
        fixed (FullTrustPipePacketHeader* pHeader = &header)
        {
            stream.ReadExactly(new(pHeader, sizeof(FullTrustPipePacketHeader)));
        }
    }

    public static void WritePacketWithJsonContent<TData>(this PipeStream stream, byte version, FullTrustPipePacketType type, FullTrustPipePacketCommand command, TData data)
    {
        FullTrustPipePacketHeader header = default;
        header.Version = version;
        header.Type = type;
        header.Command = command;
        header.ContentType = FullTrustPipePacketContentType.Json;

        stream.WritePacket(ref header, JsonSerializer.SerializeToUtf8Bytes(data));
    }

    public static void WritePacket(this PipeStream stream, ref FullTrustPipePacketHeader header, ReadOnlySpan<byte> content)
    {
        header.ContentLength = content.Length;
        header.Checksum = XxHash64.HashToUInt64(content);

        stream.WritePacket(in header);
        stream.Write(content);
    }

    public static void WritePacket(this PipeStream stream, byte version, FullTrustPipePacketType type, FullTrustPipePacketCommand command)
    {
        FullTrustPipePacketHeader header = default;
        header.Version = version;
        header.Type = type;
        header.Command = command;

        stream.WritePacket(in header);
    }

    public static unsafe void WritePacket(this PipeStream stream, ref readonly FullTrustPipePacketHeader header)
    {
        fixed (FullTrustPipePacketHeader* pHeader = &header)
        {
            stream.Write(new(pHeader, sizeof(FullTrustPipePacketHeader)));
        }
    }
}