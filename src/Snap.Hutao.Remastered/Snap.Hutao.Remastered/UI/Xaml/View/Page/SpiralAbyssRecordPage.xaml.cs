// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.SpiralAbyss;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class SpiralAbyssRecordPage : ScopedPage
{
    public SpiralAbyssRecordPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<SpiralAbyssViewModel>();
    }

    public new SpiralAbyssViewModel DataContext { get => (SpiralAbyssViewModel)base.DataContext; set => base.DataContext = value; }
}
