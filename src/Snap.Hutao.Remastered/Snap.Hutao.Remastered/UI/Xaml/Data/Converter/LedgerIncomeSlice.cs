// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace Snap.Hutao.Remastered.UI.Xaml.Data.Converter;

/// <summary>
/// 旅行者札记原石来源对应的环形图扇区及图例项。
/// </summary>
public sealed partial class LedgerIncomeSlice : ObservableObject
{
    public string Action { get; init => SetProperty(ref field, value); } = default!;

    public string FormattedNum { get; init => SetProperty(ref field, value); } = default!;

    public string FormattedPercent { get; init => SetProperty(ref field, value); } = default!;

    public Geometry Data { get; init => SetProperty(ref field, value); } = default!;

    public Brush Fill { get; init => SetProperty(ref field, value); } = default!;
}
