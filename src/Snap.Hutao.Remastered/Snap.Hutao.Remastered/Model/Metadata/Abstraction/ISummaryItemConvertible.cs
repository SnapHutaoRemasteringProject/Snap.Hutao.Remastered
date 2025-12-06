// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.ViewModel.GachaLog;

namespace Snap.Hutao.Remastered.Model.Metadata.Abstraction;

internal interface ISummaryItemConvertible
{
    QualityType Quality { get; }

    SummaryItem ToSummaryItem(int lastPull, in DateTimeOffset time, bool isUp);
}