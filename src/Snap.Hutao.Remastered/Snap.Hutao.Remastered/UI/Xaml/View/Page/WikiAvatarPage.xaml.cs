// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Wiki;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class WikiAvatarPage : ScopedPage
{
    public WikiAvatarPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<WikiAvatarViewModel>();
    }

    public new WikiAvatarViewModel DataContext { get => (WikiAvatarViewModel)base.DataContext; set => base.DataContext = value; }
}
