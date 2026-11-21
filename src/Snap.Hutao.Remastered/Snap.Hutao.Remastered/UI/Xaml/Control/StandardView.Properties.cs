// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control;

public sealed partial class StandardView
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty EmptyContentProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("EmptyContent", typeof(global::Microsoft.UI.Xaml.UIElement), typeof(StandardView), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.UIElement? EmptyContent
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.UIElement))]
        get => (global::Microsoft.UI.Xaml.UIElement)GetValue(EmptyContentProperty);
        set => SetValue(EmptyContentProperty, value);
    }
}
