// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control.Card;

public sealed partial class VerticalCard
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty TopProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("Top", typeof(global::Microsoft.UI.Xaml.UIElement), typeof(VerticalCard), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.UIElement? Top
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.UIElement))]
        get => (global::Microsoft.UI.Xaml.UIElement)GetValue(TopProperty);
        set => SetValue(TopProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty BottomProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("Bottom", typeof(global::Microsoft.UI.Xaml.UIElement), typeof(VerticalCard), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.UIElement? Bottom
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.UIElement))]
        get => (global::Microsoft.UI.Xaml.UIElement)GetValue(BottomProperty);
        set => SetValue(BottomProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty HorizontalTopAlignmentProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("HorizontalTopAlignment", typeof(global::Microsoft.UI.Xaml.HorizontalAlignment), typeof(VerticalCard), global::Microsoft.UI.Xaml.PropertyMetadata.Create((global::Microsoft.UI.Xaml.HorizontalAlignment)3));
    public global::Microsoft.UI.Xaml.HorizontalAlignment HorizontalTopAlignment
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.HorizontalAlignment))]
        get => (global::Microsoft.UI.Xaml.HorizontalAlignment)GetValue(HorizontalTopAlignmentProperty);
        set => SetValue(HorizontalTopAlignmentProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty VerticalTopAlignmentProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("VerticalTopAlignment", typeof(global::Microsoft.UI.Xaml.VerticalAlignment), typeof(VerticalCard), global::Microsoft.UI.Xaml.PropertyMetadata.Create((global::Microsoft.UI.Xaml.VerticalAlignment)3));
    public global::Microsoft.UI.Xaml.VerticalAlignment VerticalTopAlignment
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.VerticalAlignment))]
        get => (global::Microsoft.UI.Xaml.VerticalAlignment)GetValue(VerticalTopAlignmentProperty);
        set => SetValue(VerticalTopAlignmentProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty HorizontalBottomAlignmentProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("HorizontalBottomAlignment", typeof(global::Microsoft.UI.Xaml.HorizontalAlignment), typeof(VerticalCard), global::Microsoft.UI.Xaml.PropertyMetadata.Create((global::Microsoft.UI.Xaml.HorizontalAlignment)1));
    public global::Microsoft.UI.Xaml.HorizontalAlignment HorizontalBottomAlignment
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.HorizontalAlignment))]
        get => (global::Microsoft.UI.Xaml.HorizontalAlignment)GetValue(HorizontalBottomAlignmentProperty);
        set => SetValue(HorizontalBottomAlignmentProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty VerticalBottomAlignmentProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("VerticalBottomAlignment", typeof(global::Microsoft.UI.Xaml.VerticalAlignment), typeof(VerticalCard), global::Microsoft.UI.Xaml.PropertyMetadata.Create((global::Microsoft.UI.Xaml.VerticalAlignment)3));
    public global::Microsoft.UI.Xaml.VerticalAlignment VerticalBottomAlignment
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.VerticalAlignment))]
        get => (global::Microsoft.UI.Xaml.VerticalAlignment)GetValue(VerticalBottomAlignmentProperty);
        set => SetValue(VerticalBottomAlignmentProperty, value);
    }
}
