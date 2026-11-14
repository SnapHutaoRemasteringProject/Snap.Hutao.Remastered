// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control.Image;

public sealed partial class CachedImage
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty StretchProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("Stretch", typeof(global::Microsoft.UI.Xaml.Media.Stretch), typeof(CachedImage), global::Microsoft.UI.Xaml.PropertyMetadata.Create((global::Microsoft.UI.Xaml.Media.Stretch)2));
    public global::Microsoft.UI.Xaml.Media.Stretch Stretch
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.Stretch))]
        get => (global::Microsoft.UI.Xaml.Media.Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }
}
