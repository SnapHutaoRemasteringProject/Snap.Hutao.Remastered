// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact;
using Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Cultivation;
using Snap.Hutao.Remastered.Core.Text.Json;
using System.Buffers;
using System.IO.Hashing;
using System.IO.Pipes;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess;

public static class PipeStreamExtension
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

                return JsonSerialize.Deserialize<TData>(content, JsonOptions.Default);
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

            // The caller's static type is the wire contract, so no runtime Type lookup is needed.
            stream.WritePacket(ref header, JsonSerialize.SerializeToUtf8Bytes(data, JsonOptions.Default));
        }

        public void WritePacketWithJsonResponse(byte version, PipePacketType type, PipePacketCommand command, PipeResponse response)
        {
            PipePacketHeader header = default;
            header.Version = version;
            header.Type = type;
            header.Command = command;
            header.ContentType = PipePacketContentType.Json;

            // PipeResponse<T>.Data is erased if the base type is serialized directly; dispatch
            // over the closed response types that this private pipe actually writes.
            byte[] content = response switch
            {
                PipeResponse<Void> value => JsonSerialize.SerializeToUtf8Bytes(value, JsonOptions.Default),
                PipeResponse<AutomationCultivationProject> value => JsonSerialize.SerializeToUtf8Bytes(value, JsonOptions.Default),
                _ => throw new NotSupportedException($"The pipe response type '{response.GetType()}' is not registered."),
            };

            stream.WritePacket(ref header, content);
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