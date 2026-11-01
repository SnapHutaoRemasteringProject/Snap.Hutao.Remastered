// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using Snap.Hutao.Remastered.ViewModel.Abstraction;
using Snap.Hutao.Remastered.ViewModel.GachaLog;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Card;

public sealed partial class GachaStatisticsCard : MaterialCardButton
{
    public GachaStatisticsCard(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        this.InitializeViewModelSlim<ViewModel.GachaLog.GachaLogViewModelSlim>(serviceProvider);
    }

    public new GachaLogViewModelSlim DataContext { get => (GachaLogViewModelSlim)base.DataContext; set => base.DataContext = value; }
}
