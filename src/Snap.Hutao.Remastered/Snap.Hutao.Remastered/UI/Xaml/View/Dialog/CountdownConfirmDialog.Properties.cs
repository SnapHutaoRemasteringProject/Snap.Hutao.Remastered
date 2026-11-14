// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.View.Dialog;

public sealed partial class CountdownConfirmDialog
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty SeverityProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("Severity", typeof(global::Microsoft.UI.Xaml.Controls.InfoBarSeverity), typeof(CountdownConfirmDialog), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Controls.InfoBarSeverity Severity
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.InfoBarSeverity))]
        get => (global::Microsoft.UI.Xaml.Controls.InfoBarSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }
}
