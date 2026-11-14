// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.UI.Xaml;
using Snap.Hutao.Remastered.ViewModel;

namespace Snap.Hutao.Remastered.UI.Shell;

public sealed partial class NotifyIconContextMenuContent : UserControl
{
    internal NotifyIconViewModel? ViewModel => Root.DataContext as NotifyIconViewModel;

    internal FrameworkElement RootElement => Root;

    public NotifyIconContextMenuContent(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        Root.InitializeDataContext<NotifyIconViewModel>(serviceProvider);
        Bindings.Update();
    }

    private Visibility GetScreenshotVisibility() => NotifyIconViewModel.CanTakeScreenshot
        ? Visibility.Visible
        : Visibility.Collapsed;
}
