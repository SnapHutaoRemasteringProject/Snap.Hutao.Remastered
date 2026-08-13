// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;
using System.Runtime.CompilerServices;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior.Action;

public sealed class HideAttachedFlyoutAction : DependencyObject, IAction
{
    public object? Execute(object? sender, object parameter)
    {
        if (sender is not DependencyObject element)
        {
            return default;
        }

        DependencyObject? current = element;
        while (current is not null)
        {
            if (current is FlyoutPresenter)
            {
                foreach (Popup popup in VisualTreeHelper.GetOpenPopupsForXamlRoot((Unsafe.As<FrameworkElement>(element)).XamlRoot))
                {
                    if (popup.Child == current)
                    {
                        popup.IsOpen = false;
                        break;
                    }
                }

                break;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return default;
    }
}
