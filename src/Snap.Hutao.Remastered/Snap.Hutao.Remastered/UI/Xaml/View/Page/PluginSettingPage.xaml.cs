// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Navigation;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Plugin;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class PluginSettingPage : ScopedPage
{
    private static NavigationExtraData? navigationExtraData;
    public PluginSettingPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<PluginSettingViewModel>();

        if (navigationExtraData == null)
        {
            return;
        }

        if (navigationExtraData.Data is string pluginId && DataContext is PluginSettingViewModel viewModel)
        {
            viewModel.PluginId = pluginId;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        navigationExtraData = Unsafe.As<NavigationExtraData?>(e.Parameter);
    }

    public new PluginSettingViewModel DataContext { get => (PluginSettingViewModel)base.DataContext; set => base.DataContext = value; }
}
