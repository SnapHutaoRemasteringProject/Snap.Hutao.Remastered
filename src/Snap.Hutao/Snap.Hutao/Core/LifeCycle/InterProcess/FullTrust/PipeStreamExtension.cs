// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.ExceptionService;
using System.Buffers;
using System.IO.Hashing;
using System.IO.Pipes;

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

internal static class PipeStreamExtension
{
    public static unsafe void ReadPacket(this PipeStream stream, out FullTrustPipePacketHeader header)
    {
        fixed (FullTrustPipePacketHeader* pHeader = &header)
        {
            stream.ReadExactly(new(pHeader, sizeof(FullTrustPipePacketHeader)));
        }
    }
}