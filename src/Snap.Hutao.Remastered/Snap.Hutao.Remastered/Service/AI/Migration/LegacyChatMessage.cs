// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.AI.Migration;

internal sealed class LegacyChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public string? Error { get; set; }
    public long? InputTokens { get; set; }
    public long? OutputTokens { get; set; }
    public long? ReasoningTokens { get; set; }
    public long? CachedInputTokens { get; set; }
}
