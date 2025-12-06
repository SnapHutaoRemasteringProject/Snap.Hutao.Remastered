// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Wiki;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

internal sealed partial class WikiMonsterPage : ScopedPage
{
    public WikiMonsterPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<WikiMonsterViewModel>();
    }
}