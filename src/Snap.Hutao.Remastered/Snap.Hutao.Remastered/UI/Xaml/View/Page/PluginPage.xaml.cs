// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Plugin;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class PluginPage : ScopedPage
{
    public PluginPage() => InitializeComponent();

    public new PluginViewModel? DataContext => base.DataContext as PluginViewModel;

    protected override void LoadingOverride()
    {
        InitializeDataContext<PluginViewModel>();
        if (XamlRoot is { } root && DataContext is { } viewModel)
        {
            viewModel.AttachRoot(root);
        }
    }
}
