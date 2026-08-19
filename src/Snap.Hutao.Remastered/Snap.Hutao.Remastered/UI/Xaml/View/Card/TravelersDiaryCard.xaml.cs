// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.ViewModel.Abstraction;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Card;

public sealed partial class TravelersDiaryCard : Button
{
    public TravelersDiaryCard(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        this.InitializeViewModelSlim<ViewModel.TravelersDiary.TravelersDiaryViewModel>(serviceProvider);
    }
}
