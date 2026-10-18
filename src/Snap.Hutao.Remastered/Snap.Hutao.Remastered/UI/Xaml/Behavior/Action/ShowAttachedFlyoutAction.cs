// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior.Action;

[DependencyProperty<object>("Data")]
public sealed partial class ShowAttachedFlyoutAction : DependencyObject, IAction
{
    public object? Execute(object? sender, object parameter)
    {
        if (sender is FrameworkElement element)
        {
            // A card's routed pointer event can originate inside its delete
            // button. Opening the flyout then steals the press before Click.
            if (parameter is PointerRoutedEventArgs { OriginalSource: DependencyObject source })
            {
                for (DependencyObject? current = source; current is not null && current != element; current = VisualTreeHelper.GetParent(current))
                {
                    if (current is ButtonBase) return default;
                }
            }

            ShowAttachedFlyout(element, Data ?? element.DataContext);
        }

        return default;
    }

    public static void ShowAttachedFlyout(FrameworkElement element)
    {
        ShowAttachedFlyout(element, element.DataContext);
    }

    private static void ShowAttachedFlyout(FrameworkElement element, object? data)
    {
        // The Flyout is shared via StaticResource across all card templates. WinUI's
        // DataContext inheritance doesn't reliably propagate when reparenting a shared
        // flyout between different placement targets. Explicitly set the content's
        // DataContext from the tapped card's DataContext.
        if (FlyoutBase.GetAttachedFlyout(element) is Flyout { Content: FrameworkElement content })
        {
            content.DataContext = data;
            if (content is ContentControl { ContentTemplate: not null } presenter)
            {
                // Pass the template's data root directly; a shared flyout must not
                // depend on a second inherited/reflection binding to supply it.
                presenter.Content = data;
            }
        }

        FlyoutBase.ShowAttachedFlyout(element);
    }
}
