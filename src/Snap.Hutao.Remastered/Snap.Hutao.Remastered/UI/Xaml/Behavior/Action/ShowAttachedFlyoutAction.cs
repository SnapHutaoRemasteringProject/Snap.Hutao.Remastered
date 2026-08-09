// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Xaml.Interactivity;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior.Action;

public sealed class ShowAttachedFlyoutAction : DependencyObject, IAction
{
    public object? Execute(object? sender, object parameter)
    {
        if (sender is not FrameworkElement element)
        {
            return default;
        }

        // The Flyout is shared via StaticResource across all card templates. WinUI's
        // DataContext inheritance doesn't reliably propagate when reparenting a shared
        // flyout between different placement targets. Explicitly set the content's
        // DataContext from the tapped card's DataContext.
        if (FlyoutBase.GetAttachedFlyout(element) is Flyout { Content: FrameworkElement content })
        {
            content.DataContext = element.DataContext;
        }

        FlyoutBase.ShowAttachedFlyout(element);
        return default;
    }
}