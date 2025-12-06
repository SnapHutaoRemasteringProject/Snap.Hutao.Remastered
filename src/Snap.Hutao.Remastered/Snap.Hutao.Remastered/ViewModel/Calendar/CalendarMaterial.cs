// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata.Item;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Calendar;

internal sealed class CalendarMaterial
{
    public required Material Inner { get; init; }

    public required ImmutableArray<CalendarItem> Items { get; init; }

    public bool Highlight { get; set; }

    internal required RotationalMaterialIdEntry InnerEntry { get; init; }
}