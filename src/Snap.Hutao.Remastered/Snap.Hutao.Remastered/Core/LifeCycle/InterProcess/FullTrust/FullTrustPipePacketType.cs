// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.FullTrust;

internal enum FullTrustPipePacketType : byte
{
    None = 0,
    Request = 1,
    Response = 2,
    SessionTermination = 3,
}