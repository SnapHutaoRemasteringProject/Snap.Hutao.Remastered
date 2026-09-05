// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Item;

namespace Snap.Hutao.Remastered.ViewModel.Cultivation;

public sealed class StatisticsCultivateItem
{
    private readonly TimeSpan offset;
    private readonly bool isAllMaterialsOpenToday;

    private StatisticsCultivateItem(Material inner, TimeSpan offset, bool isAllMaterialsOpenToday)
    {
        Inner = inner;
        this.offset = offset;
        this.isAllMaterialsOpenToday = isAllMaterialsOpenToday;
        ExcludedFromPresentation = true;
    }

    private StatisticsCultivateItem(Material inner, Model.Entity.CultivateItem entity, TimeSpan offset, bool isAllMaterialsOpenToday)
    {
        Inner = inner;
        Count = entity.Count;
        this.offset = offset;
        this.isAllMaterialsOpenToday = isAllMaterialsOpenToday;
    }

    public Material Inner { get; }

    public uint Count { get; set; }

    public uint Current { get; set; }

    public bool IsFinished { get => Current >= Count; }

    public string FormattedCount { get => $"{Current}/{Count}"; }

    public bool IsToday { get => Inner.IsItemOfToday(offset, true, isAllMaterialsOpenToday); }

    public bool ExcludedFromPresentation { get; set; }

    public static StatisticsCultivateItem Create(Material inner, TimeSpan offset, bool isAllMaterialsOpenToday)
    {
        return new(inner, offset, isAllMaterialsOpenToday);
    }

    public static StatisticsCultivateItem Create(Material inner, Model.Entity.CultivateItem entity, TimeSpan offset, bool isAllMaterialsOpenToday)
    {
        return new(inner, entity, offset, isAllMaterialsOpenToday);
    }
}