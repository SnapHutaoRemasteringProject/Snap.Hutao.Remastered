// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.UI.Xaml.View.Window;
using Snap.Hutao.Remastered.ViewModel.Setting;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class SettingPage : ScopedPage
{
    public SettingPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<SettingViewModel>();
        this.DataContext<SettingViewModel>()?.AttachXamlElement(RootScrollViewer, GachaLogBorder);
    }

    private void OpenGlassControls(object sender, RoutedEventArgs args) => LiquidGlassSettingsWindows.OpenControls();
    private void OpenGlassEffects(object sender, RoutedEventArgs args) => LiquidGlassSettingsWindows.OpenEffects();
}
