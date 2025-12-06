// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Collection.Generic;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.ViewModel.Cultivation;

namespace Snap.Hutao.Remastered.Service.Cultivation;

internal sealed class StatisticsCultivateItemComparer : DelegatingPropertyComparer<StatisticsCultivateItem, MaterialId>
{
    private static readonly LazySlim<StatisticsCultivateItemComparer> LazyShared = new(() => new());

    private StatisticsCultivateItemComparer()
        : base(static i => i.Inner.Id, MaterialIdComparer.Shared)
    {
    }

    public static StatisticsCultivateItemComparer Shared { get => LazyShared.Value; }
}