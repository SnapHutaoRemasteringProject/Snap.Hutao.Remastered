// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.View.Specialized;

public sealed partial class SkillPivot
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty ItemTemplateProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("ItemTemplate", typeof(global::Microsoft.UI.Xaml.DataTemplate), typeof(SkillPivot), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.DataTemplate? ItemTemplate
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.DataTemplate))]
        get => (global::Microsoft.UI.Xaml.DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }
}
