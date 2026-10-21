// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Service.AI.Migration;

internal sealed class LegacyModelProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Provider { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? MaxOutputTokens { get; set; }
    public double? Temperature { get; set; }
    public string? ReasoningEffort { get; set; }
}
