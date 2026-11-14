// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Behavior;

public sealed partial class FocusAnimationBehavior
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty StoryboardProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("Storyboard", typeof(global::Microsoft.UI.Xaml.Media.Animation.Storyboard), typeof(FocusAnimationBehavior), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.Animation.Storyboard? Storyboard
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.Animation.Storyboard))]
        get => (global::Microsoft.UI.Xaml.Media.Animation.Storyboard)GetValue(StoryboardProperty);
        set => SetValue(StoryboardProperty, value);
    }
}
