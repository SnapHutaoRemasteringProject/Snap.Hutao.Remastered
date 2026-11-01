// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.GachaLog;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class GachaLogPage : ScopedPage
{
    public GachaLogPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<GachaLogViewModel>();
    }

    public new GachaLogViewModel DataContext { get => (GachaLogViewModel)base.DataContext; set => base.DataContext = value; }
}
