// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Home;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class AnnouncementPage : ScopedPage
{
    public AnnouncementPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<AnnouncementViewModel>();
    }

    public new AnnouncementViewModel DataContext { get => Unsafe.As<AnnouncementViewModel>(base.DataContext); set => base.DataContext = value; }

    // A DataTemplate has its own binding scope. Give its button the existing
    // command directly so ButtonBase retains async CanExecute handling.
    private void OnCopyCodeLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is ButtonBase button && base.DataContext is AnnouncementViewModel viewModel)
        {
            button.Command = viewModel.CopyCodeCommand;
        }
    }

    private void OnCopyCodeUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is ButtonBase button)
        {
            button.Command = null;
        }
    }
}
