// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using XamlControl = Microsoft.UI.Xaml.Controls.Control;
using XamlPanel = Microsoft.UI.Xaml.Controls.Panel;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

/// <summary>Keeps native theme-resource brushes available while a glass surface is selected.</summary>
public sealed partial class MaterialBackground : DependencyObject
{
    private static readonly HashSet<FrameworkElement> Elements = [];
    private static bool useGlass;

    public static readonly DependencyProperty NativeBrushProperty = DependencyProperty.RegisterAttached(
        "NativeBrush", typeof(Brush), typeof(MaterialBackground), new PropertyMetadata(null, OnBrushChanged));
    public static Brush? GetNativeBrush(DependencyObject target) => (Brush?)target.GetValue(NativeBrushProperty);
    public static void SetNativeBrush(DependencyObject target, Brush? value) => target.SetValue(NativeBrushProperty, value);

    public static readonly DependencyProperty GlassBrushProperty = DependencyProperty.RegisterAttached(
        "GlassBrush", typeof(Brush), typeof(MaterialBackground), new PropertyMetadata(null, OnBrushChanged));
    public static Brush? GetGlassBrush(DependencyObject target) => (Brush?)target.GetValue(GlassBrushProperty);
    public static void SetGlassBrush(DependencyObject target, Brush? value) => target.SetValue(GlassBrushProperty, value);

    public static readonly DependencyProperty NativePaneBrushProperty = DependencyProperty.RegisterAttached(
        "NativePaneBrush", typeof(Brush), typeof(MaterialBackground), new PropertyMetadata(null, OnBrushChanged));
    public static Brush? GetNativePaneBrush(DependencyObject target) => (Brush?)target.GetValue(NativePaneBrushProperty);
    public static void SetNativePaneBrush(DependencyObject target, Brush? value) => target.SetValue(NativePaneBrushProperty, value);

    public static readonly DependencyProperty GlassPaneBrushProperty = DependencyProperty.RegisterAttached(
        "GlassPaneBrush", typeof(Brush), typeof(MaterialBackground), new PropertyMetadata(null, OnBrushChanged));
    public static Brush? GetGlassPaneBrush(DependencyObject target) => (Brush?)target.GetValue(GlassPaneBrushProperty);
    public static void SetGlassPaneBrush(DependencyObject target, Brush? value) => target.SetValue(GlassPaneBrushProperty, value);

    // Retain which local values belong to this selector across unload/reload.
    // An explicit caller background or a TemplateBinding must keep precedence.
    private static readonly DependencyProperty AppliedBrushProperty = DependencyProperty.RegisterAttached(
        "AppliedBrush", typeof(Brush), typeof(MaterialBackground), new PropertyMetadata(null));
    private static readonly DependencyProperty AppliedPaneBrushProperty = DependencyProperty.RegisterAttached(
        "AppliedPaneBrush", typeof(Brush), typeof(MaterialBackground), new PropertyMetadata(null));

    public static void Apply(bool enabled)
    {
        if (XamlApplicationLifetime.Exiting) return;
        useGlass = enabled;
        foreach (FrameworkElement element in Elements.ToArray()) Update(element);
    }

    private static void OnBrushChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not FrameworkElement element) return;
        element.Loaded -= OnLoaded;
        element.Loaded += OnLoaded;
        element.Unloaded -= OnUnloaded;
        element.Unloaded += OnUnloaded;
        if (element.IsLoaded)
        {
            Elements.Add(element);
            Update(element);
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs args)
    {
        FrameworkElement element = (FrameworkElement)sender;
        Elements.Add(element);
        Update(element);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs args) => Elements.Remove((FrameworkElement)sender);

    private static void Update(FrameworkElement element)
    {
        if (XamlApplicationLifetime.Exiting) return;
        DependencyProperty? backgroundProperty = element switch
        {
            Border => Border.BackgroundProperty,
            XamlPanel => XamlPanel.BackgroundProperty,
            XamlControl => XamlControl.BackgroundProperty,
            ContentPresenter => ContentPresenter.BackgroundProperty,
            _ => null,
        };

        if (backgroundProperty is not null && GetNativeBrush(element) is { } nativeBrush && GetGlassBrush(element) is { } glassBrush)
        {
            SetBrush(element, backgroundProperty, AppliedBrushProperty, useGlass ? glassBrush : nativeBrush);
        }

        if (element is SplitView && GetNativePaneBrush(element) is { } nativePaneBrush && GetGlassPaneBrush(element) is { } glassPaneBrush)
        {
            SetBrush(element, SplitView.PaneBackgroundProperty, AppliedPaneBrushProperty, useGlass ? glassPaneBrush : nativePaneBrush);
        }
    }

    private static void SetBrush(FrameworkElement element, DependencyProperty property, DependencyProperty appliedProperty, Brush brush)
    {
        object current = element.GetValue(property);
        object applied = element.GetValue(appliedProperty);
        if (element.ReadLocalValue(property) != DependencyProperty.UnsetValue && (applied is not Brush || !ReferenceEquals(current, applied))) return;
        if (ReferenceEquals(current, brush)) return;
        element.SetValue(appliedProperty, brush);
        element.SetValue(property, brush);
    }
}
