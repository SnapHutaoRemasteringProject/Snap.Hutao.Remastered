// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using System.Buffers;
using System.IO.Hashing;
using System.IO.Pipes;

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

internal static class PipeStreamExtension
{
    extension(PipeStream stream)
    {
        public TData? ReadJsonContent<TData>(ref readonly FullTrustPipePacketHeader header)
        {
            using (IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.RentExactly(header.ContentLength))
            {
                Span<byte> content = memoryOwner.Memory.Span;
                stream.ReadExactly(content);
                HutaoException.ThrowIf(XxHash64.HashToUInt64(content) != header.Checksum, "PipePacket Content Hash incorrect");

                return JsonSerializer.Deserialize<TData>(content);
            }
        }

        public void ReadPacket<TData>(out FullTrustPipePacketHeader header, out TData? data)
            where TData : class
        {
            data = default;

            stream.ReadPacket(out header);
            if (header.ContentType is FullTrustPipePacketContentType.Json)
            {
                data = stream.ReadJsonContent<TData>(in header);
            }
        }

        public unsafe void ReadPacket(out FullTrustPipePacketHeader header)
        {
            fixed (FullTrustPipePacketHeader* pHeader = &header)
            {
                stream.ReadExactly(new(pHeader, sizeof(FullTrustPipePacketHeader)));
            }
        }

        public void WritePacketWithJsonContent<TData>(byte version, FullTrustPipePacketType type, FullTrustPipePacketCommand command, TData data)
        {
            FullTrustPipePacketHeader header = default;
            header.Version = version;
            header.Type = type;
            header.Command = command;
            header.ContentType = FullTrustPipePacketContentType.Json;

            stream.WritePacket(ref header, JsonSerializer.SerializeToUtf8Bytes(data));
        }

        public void WritePacket(ref FullTrustPipePacketHeader header, ReadOnlySpan<byte> content)
        {
            header.ContentLength = content.Length;
            header.Checksum = XxHash64.HashToUInt64(content);

            stream.WritePacket(in header);
            stream.Write(content);
        }

        public void WritePacket(byte version, FullTrustPipePacketType type, FullTrustPipePacketCommand command)
        {
            FullTrustPipePacketHeader header = default;
            header.Version = version;
            header.Type = type;
            header.Command = command;

            stream.WritePacket(in header);
        }

        public unsafe void WritePacket(ref readonly FullTrustPipePacketHeader header)
        {
            fixed (FullTrustPipePacketHeader* pHeader = &header)
            {
                stream.Write(new(pHeader, sizeof(FullTrustPipePacketHeader)));
            }
        }
    }
}