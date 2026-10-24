// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using Snap.Hutao.Remastered.ViewModel.Abstraction;
using Snap.Hutao.Remastered.ViewModel.Calendar;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Card;

public sealed partial class CalendarCard : MaterialCardButton
{
    public CalendarCard(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        this.InitializeViewModelSlim<ViewModel.Calendar.CalendarViewModel>(serviceProvider);
    }

    public new CalendarViewModel DataContext { get => (CalendarViewModel)base.DataContext; set => base.DataContext = value; }
}
