// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

[ObservableObject]
public sealed partial class StatisticsItem : Item
{
    [ObservableProperty]
    public partial int Count { get; set; }
}
