// Copyright (c) Millennium-Science-Technology-R&D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media;
using WinUI.Composition.Hlsl;
using XamlControl = Microsoft.UI.Xaml.Controls.Control;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

/// <summary>
/// Switches a material control between the liquid glass appearance and the native one
/// without replacing the element. The glass controls derive from the WinUI control they
/// stand in for, so one instance serves both materials and the bindings, focus, caret
/// and selection of that instance survive a material change.
/// </summary>
internal static class MaterialAppearance
{
    private const string StylePrefix = "MaterialNative";

    private static readonly Dictionary<FrameworkElement, ElementState> MaterialControls = [];

    internal static void Track(FrameworkElement element)
    {
        if (MaterialControls.ContainsKey(element)) return;

        // Capture what the glass control was handed by its own constructor. Every
        // LiquidGlass control assigns its GlassBrush to Background while it is created
        // and its glass template only forwards that local value, so the native
        // appearance has to take the value away again.
        bool hasLocalStyle = false;
        bool styleSetsBackground = false;
        Style? originalStyle = null;
        Brush? glassBackground = null;
        if (element is XamlControl control)
        {
            hasLocalStyle = control.ReadLocalValue(FrameworkElement.StyleProperty) != DependencyProperty.UnsetValue;
            originalStyle = control.Style;
            styleSetsBackground = StyleSetsBackground(originalStyle);
            if (control.ReadLocalValue(XamlControl.BackgroundProperty) is LiquidGlassBrush brush)
            {
                glassBackground = brush;
            }
        }

        MaterialControls[element] = new()
        {
            HasLocalStyle = hasLocalStyle,
            CallerSetsBackground = hasLocalStyle && styleSetsBackground,
            OriginalStyle = originalStyle,
            GlassBackground = glassBackground,
        };

        Apply(element);
    }

    internal static void Release(FrameworkElement element)
    {
        if (!MaterialControls.Remove(element, out ElementState? state)) return;
        if (state.BackgroundToken == 0) return;
        if (element is XamlControl control)
        {
            control.UnregisterPropertyChangedCallback(XamlControl.BackgroundProperty, state.BackgroundToken);
        }
    }

    internal static void RefreshAll()
    {
        if (XamlApplicationLifetime.Exiting) return;

        foreach (FrameworkElement element in MaterialControls.Keys.ToArray())
        {
            Apply(element);
        }
    }

    private static void Apply(FrameworkElement element)
    {
        try
        {
            ApplyCore(element);
        }
        catch (Exception ex) when (ex is not System.OutOfMemoryException)
        {
            // Restyling runs from Loaded/Unloaded handlers, so an escaping exception would
            // surface as an application-wide unhandled error and take the page down.
            Trace.WriteLine($"Material appearance failed for {element.GetType().Name}: {ex}");
        }
    }

    private static void ApplyCore(FrameworkElement element)
    {
        if (XamlApplicationLifetime.Exiting) return;
        if (element is not XamlControl control) return;
        if (!MaterialControls.TryGetValue(element, out ElementState? state)) return;

        // These kinds cannot be restyled in place: the glass slider hides the native
        // thumb and paints its own lens, the password/search boxes paint an inner
        // control, and applying a framework hyperlink style to LiquidGlassHyperlinkButton
        // ends in a fatal stowed exception (0xC000027B) as soon as the debugger surfaces
        // the framework style's FontFamily setter. Their sites host both controls through
        // MaterialSwap instead.
        switch (ControlMaterialState.LibraryKind(element))
        {
            case "Slider":
            case "PasswordBox":
            case "SearchBox":
                return;
        }

        if (IsGlass(element))
        {
            if (!ReferenceEquals(control.Style, state.OriginalStyle))
            {
                if (state.OriginalStyle is { } originalStyle) control.Style = originalStyle;
                else control.ClearValue(FrameworkElement.StyleProperty);
            }

            if (state.CallerSetsBackground)
            {
                // The caller style already paints this control. Painting the glass
                // composition brush over it would hide the accent/danger fill.
                if (control.ReadLocalValue(XamlControl.BackgroundProperty) is LiquidGlassBrush)
                {
                    control.ClearValue(XamlControl.BackgroundProperty);
                }
            }
            else if (state.GlassBackground is { } glassBackground)
            {
                // A caller-assigned local Background (a bound brush) owns the surface in
                // both materials; only the brush the control handed itself is restored.
                object local = control.ReadLocalValue(XamlControl.BackgroundProperty);
                if (local == DependencyProperty.UnsetValue || local is LiquidGlassBrush)
                {
                    control.SetValue(XamlControl.BackgroundProperty, glassBackground);
                }
            }

            return;
        }

        // A caller-provided style already describes the native control (the styles in
        // this app derive from the framework default style), so keep it and only take
        // the glass brush away. Only the implicit glass style has to be replaced.
        if (!state.HasLocalStyle)
        {
            string key = StylePrefix + ControlMaterialState.LibraryKind(element) + "Style";
            if (FindResource<Style>(key) is { } nativeStyle && !ReferenceEquals(control.Style, nativeStyle))
            {
                try
                {
                    control.Style = nativeStyle;
                }
                catch (ArgumentException ex)
                {
                    // A framework style can fail while it resolves its own setters (a
                    // missing font resource surfaces as an invalid FontFamily). Never
                    // let one control take the whole page down.
                    Trace.WriteLine($"Material style {key} failed for {element.GetType().Name}: {ex.Message}");
                }
            }
        }

        // The glass brush is a composition brush. Any template that forwards Background
        // would paint that brush (or nothing at all) instead of the theme background,
        // which is what made controls look blank in the default material.
        if (control.ReadLocalValue(XamlControl.BackgroundProperty) is LiquidGlassBrush glassBrush)
        {
            state.GlassBackground ??= glassBrush;
            control.ClearValue(XamlControl.BackgroundProperty);
        }

        // A glass control re-applies its brush from OnApplyTemplate. Assigning a style
        // above retemplates the control, so the brush can land again after this call and
        // leave a glass surface behind in the native material. Keep the native surface
        // clean until the glass material comes back.
        if (state.BackgroundToken == 0)
        {
            state.BackgroundToken = control.RegisterPropertyChangedCallback(
                XamlControl.BackgroundProperty,
                (sender, _) =>
                {
                    if (sender is not XamlControl changed) return;
                    if (IsGlass(changed)) return;
                    if (changed.ReadLocalValue(XamlControl.BackgroundProperty) is LiquidGlassBrush)
                    {
                        changed.ClearValue(XamlControl.BackgroundProperty);
                    }
                });
        }
    }

    private static bool StyleSetsBackground(Style? style)
    {
        for (Style? current = style; current is not null; current = current.BasedOn)
        {
            foreach (SetterBase setter in current.Setters)
            {
                if (setter is Setter { Property: { } property } && property == XamlControl.BackgroundProperty) return true;
            }
        }

        return false;
    }

    private static bool IsGlass(FrameworkElement element)
    {
        // A pinned profile renders glass regardless of the selected material. Preview
        // surfaces and card hosts opt in this way and must not be restyled.
        if (!string.IsNullOrEmpty(ControlMaterialState.GetProfileKind(element))) return true;

        return ControlMaterialState.IsGlassMaterial &&
               MaterialBindingSource.Current.GetTypeEnabled(ControlMaterialState.LibraryKind(element));
    }

    private static T? FindResource<T>(string key)
        where T : class
    {
        if (Application.Current is not { } application) return null;

        ResourceDictionary resources = application.Resources;
        if (resources.TryGetValue(key, out object? value) && value is T typed) return typed;

        foreach (ResourceDictionary dictionary in resources.MergedDictionaries)
        {
            if (dictionary.TryGetValue(key, out value) && value is T merged) return merged;
        }

        return null;
    }

    private sealed class ElementState
    {
        public required bool HasLocalStyle { get; init; }
        public required bool CallerSetsBackground { get; init; }
        public Style? OriginalStyle { get; init; }
        public Brush? GlassBackground { get; set; }
        public long BackgroundToken { get; set; }
    }
}