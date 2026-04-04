// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Snap.Hutao.Remastered.ViewModel;
using Snap.Hutao.Remastered.ViewModel.User;

namespace Snap.Hutao.Remastered.UI.Xaml.View;

public sealed partial class MainView : UserControl, IDataContextInitialized
{
    public MainView()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    public void OnDataContextInitialized(IServiceProvider serviceProvider)
    {
        UserView.InitializeDataContext<UserViewModel>(serviceProvider);
    }

    private void OnBackgroundActivityButtonPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (OpenedChevronAnimatedIcon is not null)
        {
            AnimatedIcon.SetState(OpenedChevronAnimatedIcon, "PointerOver");
        }

        if (ClosedChevronAnimatedIcon is not null)
        {
            AnimatedIcon.SetState(ClosedChevronAnimatedIcon, "PointerOver");
        }
    }

    private void OnBackgroundActivityButtonPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (OpenedChevronAnimatedIcon is not null)
        {
            AnimatedIcon.SetState(OpenedChevronAnimatedIcon, "Normal");
        }

        if (ClosedChevronAnimatedIcon is not null)
        {
            AnimatedIcon.SetState(ClosedChevronAnimatedIcon, "Normal");
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;
        UserView.DataContext<UserViewModel>()?.IsViewUnloaded.Value = true;
        this.DataContext<MainViewModel>()?.Uninitialize();
    }
}