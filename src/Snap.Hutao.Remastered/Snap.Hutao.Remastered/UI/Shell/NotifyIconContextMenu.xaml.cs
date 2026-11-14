// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Shell;

public sealed partial class NotifyIconContextMenu : Flyout
{
    private readonly NotifyIconContextMenuContent content;

    public NotifyIconContextMenu(IServiceProvider serviceProvider)
    {
        AllowFocusOnInteraction = false;
        InitializeComponent();
        content = new(serviceProvider);
        Content = content;

        if (content.ViewModel is { } viewModel)
        {
            viewModel.XamlRoot = content.RootElement.XamlRoot;
            viewModel.SetNotifyIconContextMenu(this, content.RootElement);
        }

        Closed += OnClosed;
    }

    private void OnClosed(object? sender, object args)
    {
        if (content.ViewModel is { } viewModel)
        {
            viewModel.NotifyIconContextMenuClosed();
        }
    }
}
