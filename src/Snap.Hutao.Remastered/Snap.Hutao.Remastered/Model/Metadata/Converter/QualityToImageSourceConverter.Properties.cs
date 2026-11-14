// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.Model.Metadata.Converter;

public sealed partial class QualityToImageSourceConverter
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty RedSourceProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("RedSource", typeof(global::Microsoft.UI.Xaml.Media.ImageSource), typeof(QualityToImageSourceConverter), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.ImageSource? RedSource
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.ImageSource))]
        get => (global::Microsoft.UI.Xaml.Media.ImageSource)GetValue(RedSourceProperty);
        set => SetValue(RedSourceProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty OrangeSourceProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("OrangeSource", typeof(global::Microsoft.UI.Xaml.Media.ImageSource), typeof(QualityToImageSourceConverter), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.ImageSource? OrangeSource
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.ImageSource))]
        get => (global::Microsoft.UI.Xaml.Media.ImageSource)GetValue(OrangeSourceProperty);
        set => SetValue(OrangeSourceProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty PurpleSourceProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("PurpleSource", typeof(global::Microsoft.UI.Xaml.Media.ImageSource), typeof(QualityToImageSourceConverter), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.ImageSource? PurpleSource
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.ImageSource))]
        get => (global::Microsoft.UI.Xaml.Media.ImageSource)GetValue(PurpleSourceProperty);
        set => SetValue(PurpleSourceProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty BlueSourceProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("BlueSource", typeof(global::Microsoft.UI.Xaml.Media.ImageSource), typeof(QualityToImageSourceConverter), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.ImageSource? BlueSource
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.ImageSource))]
        get => (global::Microsoft.UI.Xaml.Media.ImageSource)GetValue(BlueSourceProperty);
        set => SetValue(BlueSourceProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty GreenSourceProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("GreenSource", typeof(global::Microsoft.UI.Xaml.Media.ImageSource), typeof(QualityToImageSourceConverter), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.ImageSource? GreenSource
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.ImageSource))]
        get => (global::Microsoft.UI.Xaml.Media.ImageSource)GetValue(GreenSourceProperty);
        set => SetValue(GreenSourceProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty WhiteSourceProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("WhiteSource", typeof(global::Microsoft.UI.Xaml.Media.ImageSource), typeof(QualityToImageSourceConverter), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.ImageSource? WhiteSource
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.ImageSource))]
        get => (global::Microsoft.UI.Xaml.Media.ImageSource)GetValue(WhiteSourceProperty);
        set => SetValue(WhiteSourceProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty NoneSourceProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("NoneSource", typeof(global::Microsoft.UI.Xaml.Media.ImageSource), typeof(QualityToImageSourceConverter), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Media.ImageSource? NoneSource
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Media.ImageSource))]
        get => (global::Microsoft.UI.Xaml.Media.ImageSource)GetValue(NoneSourceProperty);
        set => SetValue(NoneSourceProperty, value);
    }
}
