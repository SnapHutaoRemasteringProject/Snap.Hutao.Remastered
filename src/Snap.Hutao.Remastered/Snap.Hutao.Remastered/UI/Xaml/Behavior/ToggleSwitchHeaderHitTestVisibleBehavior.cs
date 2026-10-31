// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

/// <summary>
/// Makes the header presenter of a switch accept hit tests so a click on its text
/// toggles the switch. The liquid glass switch derives from ToggleButton instead of
/// ToggleSwitch, so the behavior targets any FrameworkElement and looks the presenter
/// up by name, which both the native and the glass templates provide.
/// </summary>
public sealed class ToggleSwitchHeaderHitTestVisibleBehavior : BehaviorBase<FrameworkElement>
{
    protected override void OnAssociatedObjectLoaded()
    {
        if (AssociatedObject.FindDescendant("HeaderContentPresenter") is { } presenter)
        {
            presenter.IsHitTestVisible = true;
        }
    }
}