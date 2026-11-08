// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Wiki;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class WikiWeaponPage : ScopedPage
{
    public WikiWeaponPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<WikiWeaponViewModel>();
    }

    public new WikiWeaponViewModel DataContext { get => Unsafe.As<WikiWeaponViewModel>(base.DataContext); set => base.DataContext = value; }
}
