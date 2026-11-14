// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Behavior.Action;

public sealed partial class ShowTeachingTipAction
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty TeachingTipProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("TeachingTip", typeof(global::Microsoft.UI.Xaml.Controls.TeachingTip), typeof(ShowTeachingTipAction), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Controls.TeachingTip? TeachingTip
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.TeachingTip))]
        get => (global::Microsoft.UI.Xaml.Controls.TeachingTip)GetValue(TeachingTipProperty);
        set => SetValue(TeachingTipProperty, value);
    }
}
