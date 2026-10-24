// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using Snap.Hutao.Remastered.ViewModel.Abstraction;
using Snap.Hutao.Remastered.ViewModel.TravelersDiary;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Card;

public sealed partial class TravelersDiaryCard : MaterialCardButton
{
    public TravelersDiaryCard(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        this.InitializeViewModelSlim<ViewModel.TravelersDiary.TravelersDiaryViewModel>(serviceProvider);
    }

    public new TravelersDiaryViewModel DataContext { get => (TravelersDiaryViewModel)base.DataContext; set => base.DataContext = value; }
}
