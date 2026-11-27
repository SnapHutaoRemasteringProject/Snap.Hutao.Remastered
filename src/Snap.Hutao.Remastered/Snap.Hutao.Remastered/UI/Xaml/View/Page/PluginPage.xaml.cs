// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Plugin;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class PluginPage : ScopedPage
{
    public PluginPage() => InitializeComponent();

    public new PluginViewModel? DataContext
    {
        get => Unsafe.As<PluginViewModel>(base.DataContext);
        set => base.DataContext = value;
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<PluginViewModel>();
        if (XamlRoot is { } root && DataContext is { } viewModel)
        {
            viewModel.AttachRoot(root);
        }
    }
}
