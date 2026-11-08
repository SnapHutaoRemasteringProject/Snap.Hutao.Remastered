// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Wiki;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class WikiMonsterPage : ScopedPage
{
    public WikiMonsterPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<WikiMonsterViewModel>();
    }

    public new WikiMonsterViewModel DataContext { get => Unsafe.As<WikiMonsterViewModel>(base.DataContext); set => base.DataContext = value; }
}
