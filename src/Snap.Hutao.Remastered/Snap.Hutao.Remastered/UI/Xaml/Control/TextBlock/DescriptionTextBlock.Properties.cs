// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control.TextBlock;

public sealed partial class DescriptionTextBlock
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty TextStyleProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("TextStyle", typeof(global::Microsoft.UI.Xaml.Style), typeof(DescriptionTextBlock), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object), OnTextStyleChanged));
    public global::Microsoft.UI.Xaml.Style? TextStyle
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Style))]
        get => (global::Microsoft.UI.Xaml.Style)GetValue(TextStyleProperty);
        set => SetValue(TextStyleProperty, value);
    }
}
