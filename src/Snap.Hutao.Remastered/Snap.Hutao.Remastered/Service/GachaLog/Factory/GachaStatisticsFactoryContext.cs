// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.GachaLog.Factory;

public readonly struct GachaStatisticsFactoryContext
{
    public readonly IServiceProvider ServiceProvider;
    public readonly ImmutableArray<GachaItem> Items;
    public readonly ImmutableArray<BeyondGachaItem> BydMaterials;
    public readonly ImmutableArray<HistoryWishBuilder> HistoryWishBuilders;
    public readonly GachaLogServiceMetadataContext Metadata;
    private readonly AppOptions appOptions;

    public GachaStatisticsFactoryContext(IServiceProvider serviceProvider, ImmutableArray<GachaItem> items, ImmutableArray<BeyondGachaItem> bydMaterials, ImmutableArray<HistoryWishBuilder> historyWishBuilders, GachaLogServiceMetadataContext metadata)
    {
        ServiceProvider = serviceProvider;
        Items = items;
        BydMaterials = bydMaterials;
        HistoryWishBuilders = historyWishBuilders;
        Metadata = metadata;
        appOptions = serviceProvider.GetRequiredService<AppOptions>();
    }

    public bool IsUnobtainedWishItemVisible { get => appOptions.IsUnobtainedWishItemVisible.Value; }

    public bool IsEmptyHistoryWishVisible { get => appOptions.IsEmptyHistoryWishVisible.Value; }
}