// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using Snap.Hutao.Remastered.Resource.Localization.AI;

namespace Snap.Hutao.Remastered.ViewModel.AI;

internal sealed class AgentConversationException : Exception
{
    public AgentConversationException(SHName message)
        : base(StringResourceProxy.Default[message])
    {
        ResourceName = message;
    }

    public AgentConversationException(SHName message, Exception innerException)
        : base(StringResourceProxy.Default[message], innerException)
    {
        ResourceName = message;
    }

    public SHName ResourceName { get; }
}
