// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control.Card;

public sealed partial class CardProgressBar
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty ProgressForegroundProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("ProgressForeground", typeof(global::Microsoft.UI.Xaml.Media.Brush), typeof(CardProgressBar), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.Brush? ProgressForeground
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.Brush))]
        get => (global::Microsoft.UI.Xaml.Media.Brush)GetValue(ProgressForegroundProperty);
        set => SetValue(ProgressForegroundProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty TextForegroundProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("TextForeground", typeof(global::Microsoft.UI.Xaml.Media.Brush), typeof(CardProgressBar), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.Brush? TextForeground
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.Brush))]
        get => (global::Microsoft.UI.Xaml.Media.Brush)GetValue(TextForegroundProperty);
        set => SetValue(TextForegroundProperty, value);
    }
}
