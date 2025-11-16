// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Core.LifeCycle.InterProcess.FullTrust;

internal abstract class FullTrustResult
{
    public bool Succeeded { get; set; }

    public string? ErrorMessage { get; set; }
}