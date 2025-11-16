// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FullTrustPipePacketHeader
{
    public byte Version;
    public FullTrustPipePacketType Type;
    public FullTrustPipePacketCommand Command;
    public FullTrustPipePacketContentType ContentType;
    public int ContentLength;
    public ulong Checksum;
}