// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.ViewModel.GachaLog;

namespace Snap.Hutao.Remastered.Model.Metadata.Abstraction;

internal interface IStatisticsItemConvertible
{
    StatisticsItem ToStatisticsItem(int count);
}