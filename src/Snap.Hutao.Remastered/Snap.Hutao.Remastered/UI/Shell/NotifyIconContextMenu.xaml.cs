// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.UI.Xaml;
using Snap.Hutao.Remastered.ViewModel;

namespace Snap.Hutao.Remastered.UI.Shell;

public sealed partial class NotifyIconContextMenu : Flyout
{
    public NotifyIconContextMenu(IServiceProvider serviceProvider)
    {
        AllowFocusOnInteraction = false;
        InitializeComponent();
        Root.InitializeDataContext<NotifyIconViewModel>(serviceProvider);

        if (Root.DataContext is NotifyIconViewModel viewModel)
        {
            viewModel.XamlRoot = Root.XamlRoot;
            viewModel.SetNotifyIconContextMenu(this, Root);
        }

        Closed += OnClosed;
    }

    private void OnClosed(object? sender, object args)
    {
        if (Root.DataContext is NotifyIconViewModel viewModel)
        {
            viewModel.NotifyIconContextMenuClosed();
        }
    }
}