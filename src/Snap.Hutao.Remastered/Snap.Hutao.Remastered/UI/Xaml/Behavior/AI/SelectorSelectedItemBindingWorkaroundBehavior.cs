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
[DependencyProperty<object>("SelectedItem", PropertyChangedCallbackName = nameof(OnSelectedItemChanged))]
internal sealed partial class SelectorSelectedItemBindingWorkaroundBehavior : BehaviorBase<Selector>
{
    // ItemsSource/SelectedItem changed, updating associated object
    private bool isUpdatingAssociatedObject;

    // Associated object selection changed, updating SelectedItem
    private bool isUpdatingSelectedItem;

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
        if (d is not SelectorSelectedItemBindingWorkaroundBehavior behavior)
        {
            return;
        }

        behavior.UpdateAssociatedObject();
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SelectorSelectedItemBindingWorkaroundBehavior behavior)
        {
            return;
        }

        if (behavior.isUpdatingSelectedItem)
        {
            return;
        }

        behavior.UpdateAssociatedSelectedItem();
    }

    private void OnAssociatedObjectSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isUpdatingAssociatedObject)
        {
            return;
        }

        using (BooleanTrueScope.Create(ref isUpdatingSelectedItem))
        {
            SelectedItem = AssociatedObject.SelectedItem;
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
            AssociatedObject.ItemsSource = ItemsSource;
            AssociatedObject.SelectedItem = SelectedItem;
        }
    }

    private void UpdateAssociatedSelectedItem()
    {
        if (AssociatedObject == null)
        {
            return;
        }

        if (ReferenceEquals(AssociatedObject.SelectedItem, SelectedItem))
        {
            return;
        }

        using (BooleanTrueScope.Create(ref isUpdatingAssociatedObject))
        {
            AssociatedObject.SelectedItem = SelectedItem;
        }
    }
}
