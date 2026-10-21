// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.AI.Models;

internal sealed partial class ModelProfileAgentOptions
{
    public void CopyFrom(ModelProfileAgentOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (ReferenceEquals(this, source)) { return; }
        Temperature = source.Temperature;
        TopP = source.TopP;
        ReasoningEffort = source.ReasoningEffort;
        ThinkingEnabled = source.ThinkingEnabled;
        OmitReasoningEffortWhenThinkingDisabled = source.OmitReasoningEffortWhenThinkingDisabled;
        MaxInputTokens = source.MaxInputTokens;
        MaxContextWindowTokens = source.MaxContextWindowTokens;
        MaxOutputTokens = source.MaxOutputTokens;
    }
}
