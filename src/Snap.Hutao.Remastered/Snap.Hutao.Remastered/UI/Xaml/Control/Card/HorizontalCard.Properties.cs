// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control.Card;

public sealed partial class HorizontalCard
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty LeftProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("Left", typeof(global::Microsoft.UI.Xaml.UIElement), typeof(HorizontalCard), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.UIElement? Left
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.UIElement))]
        get => (global::Microsoft.UI.Xaml.UIElement)GetValue(LeftProperty);
        set => SetValue(LeftProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty RightProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("Right", typeof(global::Microsoft.UI.Xaml.UIElement), typeof(HorizontalCard), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.UIElement? Right
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.UIElement))]
        get => (global::Microsoft.UI.Xaml.UIElement)GetValue(RightProperty);
        set => SetValue(RightProperty, value);
    }
}
