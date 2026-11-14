// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Behaviors;
using JetBrains.Annotations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

[UsedImplicitly]
[DependencyProperty<bool>("IsBeyondMode", PropertyChangedCallbackName = nameof(OnIsBeyondModeChanged))]
public sealed partial class PivotBeyondModeBehavior : BehaviorBase<Pivot>
{
    private PivotItem? overviewItem;
    private readonly List<PivotItem> normalOnlyItems = [];
    private bool isBeyondModeInitialized;

    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.PivotItem))]
    protected override void OnAssociatedObjectLoaded()
    {
        if (overviewItem is not null)
        {
            return;
        }

        if (AssociatedObject.Items.Count == 0)
        {
            return;
        }

        overviewItem = (PivotItem)AssociatedObject.Items[0];
        for (int i = 1; i < AssociatedObject.Items.Count; i++)
        {
            normalOnlyItems.Add((PivotItem)AssociatedObject.Items[i]);
        }

        isBeyondModeInitialized = true;
    }

    private static void OnIsBeyondModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs _)
    {
        PivotBeyondModeBehavior behavior = sender.As<PivotBeyondModeBehavior>();
        if (behavior.AssociatedObject is null || behavior.overviewItem is null || !behavior.isBeyondModeInitialized)
        {
            return;
        }

        behavior.UpdatePivotItems(behavior.IsBeyondMode.GetValueOrDefault());
    }

    private void UpdatePivotItems(bool isBeyondMode)
    {
        if (AssociatedObject is null || overviewItem is null)
        {
            return;
        }

        AssociatedObject.Items.Clear();
        AssociatedObject.Items.Add(overviewItem);

        if (!isBeyondMode)
        {
            foreach (PivotItem item in normalOnlyItems)
            {
                AssociatedObject.Items.Add(item);
            }
        }
    }
}
