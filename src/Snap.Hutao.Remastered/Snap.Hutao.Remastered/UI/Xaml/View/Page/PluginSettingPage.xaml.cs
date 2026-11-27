// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Navigation;
using Snap.Hutao.Remastered.Service.Navigation;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Plugin;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class PluginSettingPage : ScopedPage
{
    private PluginSettingTarget? target;

    public PluginSettingPage() => InitializeComponent();

    public new PluginSettingViewModel? DataContext => base.DataContext as PluginSettingViewModel;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        target = (e.Parameter as INavigationExtraData)?.Data as PluginSettingTarget;
        base.OnNavigatedTo(e);
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<PluginSettingViewModel>();
        if (DataContext is { } viewModel) viewModel.Target = target;
    }
}
