// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

internal enum FullTrustPipePacketCommand : byte
{
    None = 0,
    StartProcess = 1,
    ResumeMainThread = 2,
}