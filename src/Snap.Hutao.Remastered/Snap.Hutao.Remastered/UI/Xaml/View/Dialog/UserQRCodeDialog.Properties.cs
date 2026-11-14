// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

public sealed partial class UserQRCodeDialog
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty QRCodeSourceProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("QRCodeSource", typeof(global::Microsoft.UI.Xaml.Media.ImageSource), typeof(UserQRCodeDialog), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.ImageSource? QRCodeSource
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.ImageSource))]
        get => (global::Microsoft.UI.Xaml.Media.ImageSource)GetValue(QRCodeSourceProperty);
        set => SetValue(QRCodeSourceProperty, value);
    }
}
