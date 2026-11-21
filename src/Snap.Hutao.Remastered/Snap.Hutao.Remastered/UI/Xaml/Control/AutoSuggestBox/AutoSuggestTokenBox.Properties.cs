// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

// Explicit accessors allow CsWinRT to preserve projected cast types.
// Registration names, defaults and callbacks are unchanged.
namespace Snap.Hutao.Remastered.UI.Xaml.Control.AutoSuggestBox;

public sealed partial class AutoSuggestTokenBox
{
    public static readonly global::Microsoft.UI.Xaml.DependencyProperty AutoSuggestBoxTextBoxStyleProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("AutoSuggestBoxTextBoxStyle", typeof(global::Microsoft.UI.Xaml.Style), typeof(AutoSuggestTokenBox), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Style? AutoSuggestBoxTextBoxStyle
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Style))]
        get => (global::Microsoft.UI.Xaml.Style)GetValue(AutoSuggestBoxTextBoxStyleProperty);
        set => SetValue(AutoSuggestBoxTextBoxStyleProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty AutoSuggestBoxStyleProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("AutoSuggestBoxStyle", typeof(global::Microsoft.UI.Xaml.Style), typeof(AutoSuggestTokenBox), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Style? AutoSuggestBoxStyle
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Style))]
        get => (global::Microsoft.UI.Xaml.Style)GetValue(AutoSuggestBoxStyleProperty);
        set => SetValue(AutoSuggestBoxStyleProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty QueryIconProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("QueryIcon", typeof(global::Microsoft.UI.Xaml.Controls.IconSource), typeof(AutoSuggestTokenBox), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Controls.IconSource? QueryIcon
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.IconSource))]
        get => (global::Microsoft.UI.Xaml.Controls.IconSource)GetValue(QueryIconProperty);
        set => SetValue(QueryIconProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty SuggestedItemTemplateProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("SuggestedItemTemplate", typeof(global::Microsoft.UI.Xaml.DataTemplate), typeof(AutoSuggestTokenBox), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.DataTemplate? SuggestedItemTemplate
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.DataTemplate))]
        get => (global::Microsoft.UI.Xaml.DataTemplate)GetValue(SuggestedItemTemplateProperty);
        set => SetValue(SuggestedItemTemplateProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty SuggestedItemTemplateSelectorProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("SuggestedItemTemplateSelector", typeof(global::Microsoft.UI.Xaml.Controls.DataTemplateSelector), typeof(AutoSuggestTokenBox), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Controls.DataTemplateSelector? SuggestedItemTemplateSelector
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.DataTemplateSelector))]
        get => (global::Microsoft.UI.Xaml.Controls.DataTemplateSelector)GetValue(SuggestedItemTemplateSelectorProperty);
        set => SetValue(SuggestedItemTemplateSelectorProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty SuggestedItemContainerStyleProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("SuggestedItemContainerStyle", typeof(global::Microsoft.UI.Xaml.Style), typeof(AutoSuggestTokenBox), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Style? SuggestedItemContainerStyle
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Style))]
        get => (global::Microsoft.UI.Xaml.Style)GetValue(SuggestedItemContainerStyleProperty);
        set => SetValue(SuggestedItemContainerStyleProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty TokenItemTemplateProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("TokenItemTemplate", typeof(global::Microsoft.UI.Xaml.DataTemplate), typeof(AutoSuggestTokenBox), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.DataTemplate? TokenItemTemplate
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.DataTemplate))]
        get => (global::Microsoft.UI.Xaml.DataTemplate)GetValue(TokenItemTemplateProperty);
        set => SetValue(TokenItemTemplateProperty, value);
    }

    public static readonly global::Microsoft.UI.Xaml.DependencyProperty TokenItemTemplateSelectorProperty = global::Microsoft.UI.Xaml.DependencyProperty.Register("TokenItemTemplateSelector", typeof(global::Microsoft.UI.Xaml.Controls.DataTemplateSelector), typeof(AutoSuggestTokenBox), global::Microsoft.UI.Xaml.PropertyMetadata.Create(default(object)));
    public global::Microsoft.UI.Xaml.Controls.DataTemplateSelector? TokenItemTemplateSelector
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(global::Microsoft.UI.Xaml.Controls.DataTemplateSelector))]
        get => (global::Microsoft.UI.Xaml.Controls.DataTemplateSelector)GetValue(TokenItemTemplateSelectorProperty);
        set => SetValue(TokenItemTemplateSelectorProperty, value);
    }
}
