// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Setting;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

internal sealed partial class SettingPage : ScopedPage
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
}