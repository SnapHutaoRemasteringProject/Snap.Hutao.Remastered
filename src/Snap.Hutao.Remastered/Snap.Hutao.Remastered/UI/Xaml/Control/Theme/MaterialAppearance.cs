// Copyright (c) Millennium-Science-Technology-R&D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using WinUI.Composition.Hlsl;
using WinUI.LiquidGlass;
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

    private static readonly ConditionalWeakTable<FrameworkElement, ElementState> States = new();

    private static readonly HashSet<FrameworkElement> TrackedControls = [];

    internal static void Track(FrameworkElement element)
    {
        if (TrackedControls.Add(element))
        {
            States.GetValue(element, static key => CaptureState(key));
        }

        Apply(element);
    }

    internal static void Release(FrameworkElement element)
    {
        TrackedControls.Remove(element);
        if (!States.TryGetValue(element, out ElementState? state)) return;
        if (state.BackgroundToken == 0) return;
        if (element is XamlControl control)
        {
            control.UnregisterPropertyChangedCallback(XamlControl.BackgroundProperty, state.BackgroundToken);
        }

        // ApplyCore registers the callback again when the control is tracked in the
        // native material after a reload.
        state.BackgroundToken = 0;
    }

    internal static void RefreshAll()
    {
        if (XamlApplicationLifetime.Exiting) return;

        // Do not filter on IsLoaded here. WinUI can report false for an element that
        // is still in the tree while Loaded/Unloaded are being reordered; skipping it
        // would leave that control in the previous material until its next reload.
        foreach (FrameworkElement element in TrackedControls.ToArray())
        {
            Apply(element);
        }
    }

    private static ElementState CaptureState(FrameworkElement element)
    {
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

        MotionSettings motion = new(
            LiquidGlassInteraction.GetRestScale(element),
            LiquidGlassInteraction.GetPointerOverScale(element),
            LiquidGlassInteraction.GetPressedScale(element),
            LiquidGlassInteraction.GetFocusedScale(element),
            LiquidGlassInteraction.GetElasticity(element),
            LiquidGlassInteraction.GetPointerDisplacement(element));

        return new()
        {
            HasLocalStyle = hasLocalStyle,
            CallerSetsBackground = hasLocalStyle && styleSetsBackground,
            OriginalStyle = originalStyle,
            GlassBackground = glassBackground,
            Motion = motion,
        };
    }

    private static void Apply(FrameworkElement element)
    {
        try
        {
            ApplyCore(element);
        }
        catch (Exception ex) when (ex is not System.OutOfMemoryException)
        {
            Trace.WriteLine($"Material appearance failed for {element.GetType().Name}: {ex}");
        }
    }

    private static void ApplyCore(FrameworkElement element)
    {
        if (XamlApplicationLifetime.Exiting) return;
        if (element is not XamlControl control) return;
        if (!States.TryGetValue(element, out ElementState? state)) return;

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
                if (control.ReadLocalValue(XamlControl.BackgroundProperty) is LiquidGlassBrush)
                {
                    control.ClearValue(XamlControl.BackgroundProperty);
                }
            }
            else if (state.GlassBackground is { } glassBackground)
            {
                object local = control.ReadLocalValue(XamlControl.BackgroundProperty);
                if (local == DependencyProperty.UnsetValue || local is LiquidGlassBrush)
                {
                    control.SetValue(XamlControl.BackgroundProperty, glassBackground);
                }
            }

            ApplyMotion(element, state, true);
            return;
        }

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

        ApplyMotion(element, state, false);
    }

    /// <summary>
    /// The glass library applies scale/translation springs to the control element itself
    /// (PointerMotionHelper), not to its template. Replacing the style therefore leaves
    /// the native button floating and swaying under the pointer. Neutralize those inputs
    /// in the native material and restore the captured glass values when it comes back.
    /// </summary>
    private static void ApplyMotion(FrameworkElement element, ElementState state, bool glass)
    {
        if (state.AppliedMotionGlass == glass) return;
        bool? previous = state.AppliedMotionGlass;

        MotionSettings motion = glass ? state.Motion : NeutralMotion;
        LiquidGlassInteraction.SetRestScale(element, motion.RestScale);
        LiquidGlassInteraction.SetPointerOverScale(element, motion.PointerOverScale);
        LiquidGlassInteraction.SetPressedScale(element, motion.PressedScale);
        LiquidGlassInteraction.SetFocusedScale(element, motion.FocusedScale);
        LiquidGlassInteraction.SetElasticity(element, motion.Elasticity);
        LiquidGlassInteraction.SetPointerDisplacement(element, motion.PointerDisplacement);

        if (!glass)
        {
            ResetMotion(element, Vector3.One);
            state.AppliedMotionGlass = glass;
            return;
        }

        if (previous == false)
        {
            float rest = (float)motion.RestScale;
            ResetMotion(element, new Vector3(rest, rest, 1.0f));
        }

        state.AppliedMotionGlass = glass;
    }

    private static void ResetMotion(FrameworkElement element, Vector3 scale)
    {
        Visual visual = ElementCompositionPreview.GetElementVisual(element);
        visual.StopAnimation("Scale");
        visual.StopAnimation("Translation");
        visual.Scale = scale;
        element.Scale = scale;
        element.Translation = Vector3.Zero;
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

    internal static bool IsGlass(FrameworkElement element)
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

    private readonly record struct MotionSettings(
        double RestScale,
        double PointerOverScale,
        double PressedScale,
        double FocusedScale,
        double Elasticity,
        double PointerDisplacement);

    private static readonly MotionSettings NeutralMotion = new(1.0, 1.0, 1.0, 1.0, 0.0, 0.0);

    private sealed class ElementState
    {
        public required bool HasLocalStyle { get; init; }
        public required bool CallerSetsBackground { get; init; }
        public required MotionSettings Motion { get; init; }
        public Style? OriginalStyle { get; init; }
        public Brush? GlassBackground { get; set; }
        public long BackgroundToken { get; set; }
        public bool? AppliedMotionGlass { get; set; }
    }
}