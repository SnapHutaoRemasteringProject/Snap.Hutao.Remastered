// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

public sealed partial class GachaStatisticsSlim : ObservableObject
{
    public required string Uid { get; init => SetProperty(ref field, value); }

    public required TypedWishSummarySlim AvatarWish { get; init => SetProperty(ref field, value); }

    public required TypedWishSummarySlim WeaponWish { get; init => SetProperty(ref field, value); }

    public required TypedWishSummarySlim StandardWish { get; init => SetProperty(ref field, value); }
}
