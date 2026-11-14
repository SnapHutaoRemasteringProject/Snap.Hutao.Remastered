// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control;

public sealed partial class ControlHelper
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty BackgroundProperty = global::Microsoft.UI.Xaml.DependencyProperty.RegisterAttached("Background", typeof(global::Microsoft.UI.Xaml.Media.Brush), typeof(ControlHelper), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object), OnBackgroundChanged));
    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.Brush))]
    public static global::Microsoft.UI.Xaml.Media.Brush? GetBackground(global::Microsoft.UI.Xaml.Controls.Control obj)
    {
        global::System.ArgumentNullException.ThrowIfNull(obj);
        return (global::Microsoft.UI.Xaml.Media.Brush? )obj.GetValue(BackgroundProperty);
    }

    public static void SetBackground(global::Microsoft.UI.Xaml.Controls.Control obj, global::Microsoft.UI.Xaml.Media.Brush? value)
    {
        global::System.ArgumentNullException.ThrowIfNull(obj);
        obj.SetValue(BackgroundProperty, value);
    }
}
