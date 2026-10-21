// Copyright (c) 2026 DGP Studio. All rights reserved.
// Licensed under the MIT license.
// Adapted from Snap.Nicole.

using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Snap.Hutao.Remastered.Core.AI;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior.AI;

[DependencyProperty<object>("ItemsSource", PropertyChangedCallbackName = nameof(OnItemsSourceChanged))]
[DependencyProperty<object>("SelectedValue", PropertyChangedCallbackName = nameof(OnSelectedValueChanged))]
[DependencyProperty<string>("SelectedValuePath", PropertyChangedCallbackName = nameof(OnSelectedValuePathChanged))]
internal sealed partial class SelectorSelectedValueBindingWorkaroundBehavior : BehaviorBase<Selector>
{
    // ItemsSource/SelectedValue/SelectedValuePath changed, updating associated object
    private bool isUpdatingAssociatedObject;

    // Associated object selection changed, updating SelectedValue
    private bool isUpdatingSelectedValue;

    protected override bool Initialize()
    {
        if (!base.Initialize())
        {
            return false;
        }

        AssociatedObject.SelectionChanged += OnAssociatedObjectSelectionChanged;
        UpdateAssociatedObject();
        return true;
    }

    protected override void OnAssociatedObjectLoaded()
    {
        UpdateAssociatedObject();
    }

    protected override bool Uninitialize()
    {
        AssociatedObject.SelectionChanged -= OnAssociatedObjectSelectionChanged;
        return base.Uninitialize();
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SelectorSelectedValueBindingWorkaroundBehavior behavior)
        {
            return;
        }

        behavior.UpdateAssociatedObject();
    }

    private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SelectorSelectedValueBindingWorkaroundBehavior behavior)
        {
            return;
        }

        if (behavior.isUpdatingSelectedValue)
        {
            return;
        }

        behavior.UpdateAssociatedSelectedValue();
    }

    private static void OnSelectedValuePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SelectorSelectedValueBindingWorkaroundBehavior behavior)
        {
            return;
        }

        behavior.UpdateAssociatedObject();
    }

    private void OnAssociatedObjectSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isUpdatingAssociatedObject)
        {
            return;
        }

        using (BooleanTrueScope.Create(ref isUpdatingSelectedValue))
        {
            SelectedValue = AssociatedObject.SelectedValue;
        }
    }

    private void UpdateAssociatedObject()
    {
        if (AssociatedObject == null)
        {
            return;
        }

        using (BooleanTrueScope.Create(ref isUpdatingAssociatedObject))
        {
            AssociatedObject.SelectedValuePath = SelectedValuePath ?? string.Empty;
            AssociatedObject.ItemsSource = ItemsSource;
            AssociatedObject.SelectedValue = SelectedValue;
        }
    }

    private void UpdateAssociatedSelectedValue()
    {
        if (AssociatedObject == null)
        {
            return;
        }

        if (Equals(AssociatedObject.SelectedValue, SelectedValue))
        {
            return;
        }

        using (BooleanTrueScope.Create(ref isUpdatingAssociatedObject))
        {
            AssociatedObject.SelectedValue = SelectedValue;
        }
    }
}
