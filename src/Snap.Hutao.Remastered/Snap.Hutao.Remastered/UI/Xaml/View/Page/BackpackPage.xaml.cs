// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Backpack;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class BackpackPage : ScopedPage
{
    public BackpackPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<BackpackViewModel>();
    }
}
