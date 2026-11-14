// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control.Effect;

public partial class TextMorphEffect
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty EasingProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("Easing", typeof(global::Microsoft.UI.Xaml.Media.Animation.EasingMode), typeof(TextMorphEffect), global::Microsoft.UI.Xaml.PropertyMetadata.Create((global::Microsoft.UI.Xaml.Media.Animation.EasingMode)2, OnAnimationChanged));
    public global::Microsoft.UI.Xaml.Media.Animation.EasingMode Easing
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.Animation.EasingMode))]
        get => (global::Microsoft.UI.Xaml.Media.Animation.EasingMode)GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty DirectionProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("Direction", typeof(global::Microsoft.Graphics.Canvas.Text.CanvasTextDirection), typeof(TextMorphEffect), global::Microsoft.UI.Xaml.PropertyMetadata.Create((global::Microsoft.Graphics.Canvas.Text.CanvasTextDirection)0, OnAnimationChanged));
    public global::Microsoft.Graphics.Canvas.Text.CanvasTextDirection Direction
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.Graphics.Canvas.Text.CanvasTextDirection))]
        get => (global::Microsoft.Graphics.Canvas.Text.CanvasTextDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty EffectVerticalAlignmentProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("EffectVerticalAlignment", typeof(global::Microsoft.Graphics.Canvas.Text.CanvasVerticalAlignment), typeof(TextMorphEffect), global::Microsoft.UI.Xaml.PropertyMetadata.Create((global::Microsoft.Graphics.Canvas.Text.CanvasVerticalAlignment)2, OnAnimationChanged));
    public global::Microsoft.Graphics.Canvas.Text.CanvasVerticalAlignment EffectVerticalAlignment
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.Graphics.Canvas.Text.CanvasVerticalAlignment))]
        get => (global::Microsoft.Graphics.Canvas.Text.CanvasVerticalAlignment)GetValue(EffectVerticalAlignmentProperty);
        set => SetValue(EffectVerticalAlignmentProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty EffectHorizontalAlignmentProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("EffectHorizontalAlignment", typeof(global::Microsoft.Graphics.Canvas.Text.CanvasHorizontalAlignment), typeof(TextMorphEffect), global::Microsoft.UI.Xaml.PropertyMetadata.Create((global::Microsoft.Graphics.Canvas.Text.CanvasHorizontalAlignment)2, OnAnimationChanged));
    public global::Microsoft.Graphics.Canvas.Text.CanvasHorizontalAlignment EffectHorizontalAlignment
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.Graphics.Canvas.Text.CanvasHorizontalAlignment))]
        get => (global::Microsoft.Graphics.Canvas.Text.CanvasHorizontalAlignment)GetValue(EffectHorizontalAlignmentProperty);
        set => SetValue(EffectHorizontalAlignmentProperty, value);
    }
}
