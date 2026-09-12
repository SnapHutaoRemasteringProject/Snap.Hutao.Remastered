// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Media;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.Converter;

/// <summary>
/// 旅行者札记原石来源对应的环形图扇区及图例项。
/// </summary>
public sealed class LedgerIncomeSlice
{
    public string Action { get; init; } = default!;

    public string FormattedNum { get; init; } = default!;

    public string FormattedPercent { get; init; } = default!;

    public Geometry Data { get; init; } = default!;

    public Brush Fill { get; init; } = default!;
}
