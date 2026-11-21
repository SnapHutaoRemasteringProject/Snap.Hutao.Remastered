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
/// <remarks>
/// A liquid glass switch has no template children yet when Loaded is raised, so the
/// presenter cannot be resolved there. Wait for the first layout pass instead, and
/// re-arm whenever the template is replaced: MaterialAppearance assigns the native
/// style to the same instance, which retemplates the control and creates a new
/// presenter that starts hit-test invisible again.
/// </remarks>
public sealed class ToggleSwitchHeaderHitTestVisibleBehavior : BehaviorBase<FrameworkElement>
{
    private bool isWaitingForTemplate;

    protected override void OnAssociatedObjectLoaded()
    {
        AssociatedObject.RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Controls.Control.TemplateProperty, OnTemplateChanged);
        WaitForTemplate();
    }

    protected override void OnAssociatedObjectUnloaded()
    {
        StopWaiting();
    }

    private void OnTemplateChanged(DependencyObject sender, DependencyProperty property)
    {
        WaitForTemplate();
    }

    private void WaitForTemplate()
    {
        if (isWaitingForTemplate) return;
        isWaitingForTemplate = true;
        AssociatedObject.LayoutUpdated += OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, object e)
    {
        if (AssociatedObject.FindDescendant("HeaderContentPresenter") is { } presenter)
        {
            presenter.IsHitTestVisible = true;
            StopWaiting();
        }
    }

    private void StopWaiting()
    {
        if (!isWaitingForTemplate) return;
        isWaitingForTemplate = false;
        AssociatedObject.LayoutUpdated -= OnLayoutUpdated;
    }
}
