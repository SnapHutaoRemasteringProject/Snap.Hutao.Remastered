// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace Snap.Hutao.Remastered.UI.Xaml.Behavior.Action;

[DependencyProperty<AnimationSet>("Animation")]
public sealed partial class StartAnimationActionNoThrow : DependencyObject, IAction
{
    /// <inheritdoc/>
    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.UIElement))]
    public object Execute(object sender, object parameter)
    {
        if (Animation is not null)
        {
            if (TargetObject is not null)
            {
                Animation.Start(TargetObject);
            }
            else
            {
                Animation.Start(sender as UIElement);
            }
        }

        return default!;
    }
}