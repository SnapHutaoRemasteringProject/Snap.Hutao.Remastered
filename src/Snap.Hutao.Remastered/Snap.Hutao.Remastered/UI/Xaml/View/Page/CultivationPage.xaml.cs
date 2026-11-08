// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Cultivation;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class CultivationPage : ScopedPage
{
    public CultivationPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<CultivationViewModel>();
    }

    public new CultivationViewModel DataContext { get => Unsafe.As<CultivationViewModel>(base.DataContext); set => base.DataContext = value; }
}
