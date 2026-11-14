// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control.AutoSuggestBox;

public partial class AutoSuggestTokenBoxItem
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty ClearButtonStyleProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("ClearButtonStyle", typeof(global::Microsoft.UI.Xaml.Style), typeof(AutoSuggestTokenBoxItem), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Style? ClearButtonStyle
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Style))]
        get => (global::Microsoft.UI.Xaml.Style)GetValue(ClearButtonStyleProperty);
        set => SetValue(ClearButtonStyleProperty, value);
    }
}
