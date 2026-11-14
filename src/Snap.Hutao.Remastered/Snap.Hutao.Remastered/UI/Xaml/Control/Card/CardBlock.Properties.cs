// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control.Card;

public sealed partial class CardBlock
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty DotVisibilityProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("DotVisibility", typeof(global::Microsoft.UI.Xaml.Visibility), typeof(CardBlock), global::Microsoft.UI.Xaml.PropertyMetadata.Create((global::Microsoft.UI.Xaml.Visibility)1));
    public global::Microsoft.UI.Xaml.Visibility DotVisibility
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Visibility))]
        get => (global::Microsoft.UI.Xaml.Visibility)GetValue(DotVisibilityProperty);
        set => SetValue(DotVisibilityProperty, value);
    }
}
