// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.AI.Migration;

internal sealed class LegacyConversation
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string InputText { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Snapshot { get; set; }
    public List<LegacyChatMessage> Messages { get; set; } = [];
}
