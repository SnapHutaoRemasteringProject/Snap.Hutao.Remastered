// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

[SuppressMessage("", "SH001")]
[DependencyProperty<CornerRadius>("PaneCornerRadius", PropertyChangedCallbackName = nameof(OnPaneCornerRadiusChanged), IsAttached = true, TargetType = typeof(NavigationView), NotNull = true)]
public sealed partial class NavigationViewHelper
{
    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.NavigationView))]
    private static void OnPaneCornerRadiusChanged(DependencyObject dp, DependencyPropertyChangedEventArgs args)
    {
        NavigationView navigationView = (NavigationView)dp;
        CornerRadius newValue = (CornerRadius)args.NewValue;

        if (navigationView.IsLoaded)
        {
            SetLoadedNavigationViewPaneCornerRadius(navigationView, newValue);
            return;
        }

        navigationView.Loaded += SetNavigationViewPaneCornerRadius;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.NavigationView))]
    private static void SetNavigationViewPaneCornerRadius(object sender, RoutedEventArgs args)
    {
        NavigationView navigationView = (NavigationView)sender;
        CornerRadius value = GetPaneCornerRadius(navigationView);
        SetLoadedNavigationViewPaneCornerRadius(navigationView, value);

        navigationView.Loaded -= SetNavigationViewPaneCornerRadius;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.SplitView))]
    private static void SetLoadedNavigationViewPaneCornerRadius(NavigationView navigationView, CornerRadius value)
    {
        if (navigationView.FindDescendant("RootSplitView") is SplitView splitView)
        {
            splitView.CornerRadius = value;
        }
    }
}