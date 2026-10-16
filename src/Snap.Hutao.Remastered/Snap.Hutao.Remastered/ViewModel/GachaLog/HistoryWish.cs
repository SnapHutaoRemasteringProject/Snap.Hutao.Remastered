// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

public sealed partial class HistoryWish : Wish, IPropertyValuesProvider
{
    public required string Version { get; init => SetProperty(ref field, value); }

    public required Uri BannerImage { get; init => SetProperty(ref field, value); }

    public required ImmutableArray<StatisticsItem> OrangeUpList { get; init => SetProperty(ref field, value); }

    public required ImmutableArray<StatisticsItem> PurpleUpList { get; init => SetProperty(ref field, value); }

    public required ImmutableArray<StatisticsItem> SummaryList { get; init => SetProperty(ref field, value); }

    public required ImmutableArray<StatisticsItem> PurpleList { get; init => SetProperty(ref field, value); }

    public required ImmutableArray<StatisticsItem> BlueList { get; init => SetProperty(ref field, value); }
}
