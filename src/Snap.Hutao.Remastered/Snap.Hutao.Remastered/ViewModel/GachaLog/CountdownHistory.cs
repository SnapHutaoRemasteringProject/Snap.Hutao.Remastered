// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Metadata;

namespace Snap.Hutao.Remastered.ViewModel.GachaLog;

public sealed partial class CountdownHistory : ObservableObject
{
    public CountdownHistory(GachaEvent gachaEvent)
    {
        LastTime = gachaEvent.To;
        FormattedTime = $"{gachaEvent.To:yyyy-MM-dd}";
        FormattedVersionOrder = $"{gachaEvent.Version} {(gachaEvent.Order is 1 ? SH.ViewModelGachaLogCountdownOrderUp : SH.ViewModelGachaLogCountdownOrderDown)}";
        Banner = gachaEvent.Banner;
    }

    public string FormattedTime { get; private set => SetProperty(ref field, value); }

    public string FormattedVersionOrder { get; private set => SetProperty(ref field, value); }

    public Uri Banner { get; private set => SetProperty(ref field, value); }

    public DateTimeOffset LastTime { get; private set => SetProperty(ref field, value); }
}
