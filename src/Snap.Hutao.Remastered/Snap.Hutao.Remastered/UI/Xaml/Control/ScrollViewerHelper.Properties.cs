// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control;

public sealed partial class ScrollViewerHelper
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty RightPanelProperty = global::Microsoft.UI.Xaml.DependencyProperty.RegisterAttached("RightPanel", typeof(global::Microsoft.UI.Xaml.UIElement), typeof(ScrollViewerHelper), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.UIElement))]
    public static global::Microsoft.UI.Xaml.UIElement? GetRightPanel(global::Microsoft.UI.Xaml.Controls.ScrollViewer obj)
    {
        global::System.ArgumentNullException.ThrowIfNull(obj);
        return (global::Microsoft.UI.Xaml.UIElement? )obj.GetValue(RightPanelProperty);
    }

    public static void SetRightPanel(global::Microsoft.UI.Xaml.Controls.ScrollViewer obj, global::Microsoft.UI.Xaml.UIElement? value)
    {
        global::System.ArgumentNullException.ThrowIfNull(obj);
        obj.SetValue(RightPanelProperty, value);
    }
}
