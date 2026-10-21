// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.AI.Models;

internal sealed partial class AppAgentOptions
{
    public void CopyFrom(AppAgentOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (ReferenceEquals(this, source)) { return; }
        UserName = source.UserName;
        SystemPrompt = source.SystemPrompt;
        MaximumIterationsPerRequest = source.MaximumIterationsPerRequest;
    }
}
