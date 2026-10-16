// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Snap.Hutao.Remastered.UI.Xaml;
using WinRT;

namespace Snap.Hutao.Remastered.ViewModel.Abstraction;

public static class ViewModelSlimExtension
{
    extension(FrameworkElement frameworkElement)
    {
        public void InitializeViewModelSlim<TDataContext>(IServiceProvider serviceProvider)
            where TDataContext : ViewModelSlim
        {
            frameworkElement.Loaded += OnFrameworkElementLoaded;
            frameworkElement.Unloaded += OnFrameworkElementUnloaded;
            frameworkElement.InitializeDataContext<TDataContext>(serviceProvider);
        }
    }

    private static void OnFrameworkElementLoaded(object sender, RoutedEventArgs e)
    {
        FrameworkElement frameworkElement = sender.As<FrameworkElement>();
        frameworkElement.DataContext<ViewModelSlim>()?.IsViewUnloaded.Value = false;
    }

    private static void OnFrameworkElementUnloaded(object sender, RoutedEventArgs e)
    {
        FrameworkElement frameworkElement = sender.As<FrameworkElement>();
        frameworkElement.DataContext<ViewModelSlim>()?.IsViewUnloaded.Value = true;
    }
}
