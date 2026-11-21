// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

/// <summary>
/// Enables hit testing for interactive switch headers.
/// </summary>
[DependencyProperty<bool>("IsHeaderHitTestVisible", DefaultValue = false, IsAttached = true, TargetType = typeof(FrameworkElement), NotNull = true)]
public sealed partial class ToggleSwitchHeaderHitTestVisibleBehavior : BehaviorBase<FrameworkElement>
{
    protected override void OnAssociatedObjectLoaded()
    {
        // The native appearance can replace its template after Loaded or on a theme change.
        // Set the value on the switch so every new native header presenter receives it.
        SetIsHeaderHitTestVisible(AssociatedObject, true);

        // The library's glass template does not bind the attached property.
        if (AssociatedObject.FindDescendant("HeaderContentPresenter") is { } presenter)
        {
            presenter.IsHitTestVisible = true;
        }
    }
}
