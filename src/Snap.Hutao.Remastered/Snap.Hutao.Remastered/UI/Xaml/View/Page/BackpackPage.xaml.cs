// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Snap.Hutao.Remastered.UI.Xaml.Behavior.Action;
using Snap.Hutao.Remastered.UI.Xaml.Control;
using Snap.Hutao.Remastered.ViewModel.Backpack;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Page;

public sealed partial class BackpackPage : ScopedPage
{
    public BackpackPage()
    {
        InitializeComponent();
    }

    protected override void LoadingOverride()
    {
        InitializeDataContext<BackpackViewModel>();
    }

    private void OnPurchasedAppendPropIconTapped(object sender, TappedRoutedEventArgs e)
    {
        // The whole card opens the item detail flyout on Tapped, stop it from replacing this one.
        e.Handled = true;

        if (sender is FrameworkElement element)
        {
            ShowAttachedFlyoutAction.ShowAttachedFlyout(element);
        }
    }
}
