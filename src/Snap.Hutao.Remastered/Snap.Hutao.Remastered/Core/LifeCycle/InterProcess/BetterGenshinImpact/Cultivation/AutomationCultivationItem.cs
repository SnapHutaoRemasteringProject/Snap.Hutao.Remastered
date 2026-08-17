// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Core.LifeCycle.InterProcess.BetterGenshinImpact.Cultivation;

public sealed class AutomationCultivationItem
{
    public required uint ItemId { get; set; }

    public required string Name { get; set; }

    public required uint Count { get; set; }

    public uint RankLevel { get; set; }

    public ImmutableArray<string> Monsters { get; set; } = ImmutableArray<string>.Empty;
}