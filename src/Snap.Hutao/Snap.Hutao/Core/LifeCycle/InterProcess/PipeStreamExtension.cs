// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using System.Buffers;
using System.IO.Hashing;
using System.IO.Pipes;

namespace Snap.Hutao.Core.LifeCycle.InterProcess;

internal static class PipeStreamExtension
{
    extension(PipeStream stream)
    {
        public TData? ReadJsonContent<TData>(ref readonly PipePacketHeader header)
        {
            using (IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.RentExactly(header.ContentLength))
            {
                Span<byte> content = memoryOwner.Memory.Span;
                stream.ReadExactly(content);
                HutaoException.ThrowIf(XxHash64.HashToUInt64(content) != header.Checksum, "PipePacket Content Hash incorrect");

                return JsonSerializer.Deserialize<TData>(content);
            }
        }

        public void ReadPacket<TData>(out PipePacketHeader header, out TData? data)
            where TData : class
        {
            data = default;

            stream.ReadPacket(out header);
            if (header.ContentType is PipePacketContentType.Json)
            {
                data = stream.ReadJsonContent<TData>(in header);
            }
        }

        public unsafe void ReadPacket(out PipePacketHeader header)
        {
            fixed (PipePacketHeader* pHeader = &header)
            {
                stream.ReadExactly(new(pHeader, sizeof(PipePacketHeader)));
            }
        }

        public void WritePacketWithJsonContent<TData>(byte version, PipePacketType type, PipePacketCommand command, TData data)
        {
            PipePacketHeader header = default;
            header.Version = version;
            header.Type = type;
            header.Command = command;
            header.ContentType = PipePacketContentType.Json;

            stream.WritePacket(ref header, JsonSerializer.SerializeToUtf8Bytes(data));
        }

        public void WritePacket(ref PipePacketHeader header, ReadOnlySpan<byte> content)
        {
            header.ContentLength = content.Length;
            header.Checksum = XxHash64.HashToUInt64(content);

            stream.WritePacket(in header);
            stream.Write(content);
        }

        public void WritePacket(byte version, PipePacketType type, PipePacketCommand command)
        {
            PipePacketHeader header = default;
            header.Version = version;
            header.Type = type;
            header.Command = command;

            stream.WritePacket(in header);
        }

        public unsafe void WritePacket(ref readonly PipePacketHeader header)
        {
            fixed (PipePacketHeader* pHeader = &header)
            {
                stream.Write(new(pHeader, sizeof(PipePacketHeader)));
            }
        }
    }
}