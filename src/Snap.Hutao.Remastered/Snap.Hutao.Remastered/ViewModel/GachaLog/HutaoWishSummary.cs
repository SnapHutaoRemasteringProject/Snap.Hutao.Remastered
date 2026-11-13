// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

public sealed class HutaoWishSummary
{
    [SetsRequiredMembers]
    public HutaoWishSummary(GachaEvent gachaEvent, ImmutableArray<StatisticsItem> upItems, ImmutableArray<StatisticsItem> orangeItems, ImmutableArray<StatisticsItem> purpleItems, ImmutableArray<StatisticsItem> blueItems)
    {
        Event = gachaEvent;
        UpItems = upItems;
        OrangeItems = orangeItems;
        PurpleItems = purpleItems;
        BlueItems = blueItems;
    }

    public required GachaEvent Event { get; init; }

    public required ImmutableArray<StatisticsItem> UpItems { get; init; }

    public required ImmutableArray<StatisticsItem> OrangeItems { get; init; }

    public required ImmutableArray<StatisticsItem> PurpleItems { get; init; }

    public required ImmutableArray<StatisticsItem> BlueItems { get; init; }
}
