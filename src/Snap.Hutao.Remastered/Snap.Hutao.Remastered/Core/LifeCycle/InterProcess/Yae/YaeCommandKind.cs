// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.Yae;

public enum YaeCommandKind : byte
{
    None = 0,
    ResponsePlayerProp = 0x03,
    ResponsePacket = 0x04,
    RequestPacketList = 0xFA,
    RequestPlayerPropList = 0xFB,
    RequestRva = 0xFD,
    RequestResumeThread = 0xFE,
    SessionEnd = 0xFF,
}
