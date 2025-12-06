// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.AvatarProperty;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

internal sealed partial class AvatarPropertyPage : ScopedPage
{
    public AvatarPropertyPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<AvatarPropertyViewModel>();
    }
}