// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.HutaoPassport;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class HutaoPassportPage : ScopedPage
{
    public HutaoPassportPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<HutaoPassportViewModel>();
    }

    public new HutaoPassportViewModel DataContext { get => Unsafe.As<HutaoPassportViewModel>(base.DataContext); set => base.DataContext = value; }
}
