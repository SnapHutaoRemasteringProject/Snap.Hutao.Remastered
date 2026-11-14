// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Behavior.Action;

public sealed partial class StartAnimationActionNoThrow
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty TargetObjectProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("TargetObject", typeof(global::Microsoft.UI.Xaml.UIElement), typeof(StartAnimationActionNoThrow), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.UIElement? TargetObject
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.UIElement))]
        get => (global::Microsoft.UI.Xaml.UIElement)GetValue(TargetObjectProperty);
        set => SetValue(TargetObjectProperty, value);
    }
}
