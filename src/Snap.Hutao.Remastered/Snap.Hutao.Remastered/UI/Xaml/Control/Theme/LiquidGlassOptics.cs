// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using System.Globalization;
using WinUI.Composition.Hlsl;
using WinUI.LiquidGlass;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

public sealed record LiquidGlassDescriptor(string Key, string EnglishLabel, double Minimum, double Maximum, double Step,
    Func<LiquidGlassBrush, double> Read, Action<LiquidGlassBrush, double> Write);

public static class LiquidGlassOptics
{
    public static readonly LiquidGlassDescriptor[] Descriptors =
    [
        new("BlurRadius", "Blur radius", 0.0, 32.0, 0.1, static b => b.BlurRadius, static (b, v) => b.BlurRadius = v),
        new("RefractionStrength", "Refraction", 0.0, 64.0, 0.5, static b => b.RefractionStrength, static (b, v) => b.RefractionStrength = v),
        new("DispersionStrength", "Chromatic dispersion", 0.0, 8.0, 0.05, static b => b.DispersionStrength, static (b, v) => b.DispersionStrength = v),
        new("HighlightStrength", "Highlights", 0.0, 2.0, 0.05, static b => b.HighlightStrength, static (b, v) => b.HighlightStrength = v),
        new("HighlightSharpness", "Highlight sharpness", 0.1, 4.0, 0.1, static b => b.HighlightSharpness, static (b, v) => b.HighlightSharpness = v),
        new("SpecularSaturation", "Specular saturation", 0.0, 10.0, 0.1, static b => b.SpecularSaturation, static (b, v) => b.SpecularSaturation = v),
        new("SpecularWidth", "Specular width", 0.1, 4.0, 0.1, static b => b.SpecularWidth, static (b, v) => b.SpecularWidth = v),
        new("RefractiveIndex", "Refractive index", 1.0, 2.0, 0.01, static b => b.RefractiveIndex, static (b, v) => b.RefractiveIndex = v),
        new("TintOpacity", "Tint opacity", 0.0, 1.0, 0.01, static b => b.TintOpacity, static (b, v) => b.TintOpacity = v),
        new("InnerShadowStrength", "Inner shadow", 0.0, 1.0, 0.01, static b => b.InnerShadowStrength, static (b, v) => b.InnerShadowStrength = v),
        new("Saturation", "Saturation", 0.0, 2.0, 0.05, static b => b.Saturation, static (b, v) => b.Saturation = v),
        new("Contrast", "Contrast", 0.0, 2.0, 0.05, static b => b.Contrast, static (b, v) => b.Contrast = v),
        new("Exposure", "Exposure", -2.0, 2.0, 0.05, static b => b.Exposure, static (b, v) => b.Exposure = v),
        new("MaterialOpacity", "Material opacity", 0.1, 1.0, 0.01, static b => b.MaterialOpacity, static (b, v) => b.MaterialOpacity = v),
        new("DispersionRange", "Dispersion range", 0.0, 1.0, 0.05, static b => b.DispersionRange, static (b, v) => b.DispersionRange = v),
        new("RefFresnelRange", "Fresnel range", 0.0, 100.0, 1, static b => b.RefFresnelRange, static (b, v) => b.RefFresnelRange = v),
        new("RefFresnelHardness", "Fresnel hardness", 0.0, 100.0, 1, static b => b.RefFresnelHardness, static (b, v) => b.RefFresnelHardness = v),
        new("RefFresnelFactor", "Fresnel intensity", 0.0, 100.0, 1, static b => b.RefFresnelFactor, static (b, v) => b.RefFresnelFactor = v),
        new("GlareRange", "Glare range", 0.0, 100.0, 1, static b => b.GlareRange, static (b, v) => b.GlareRange = v),
        new("GlareHardness", "Glare hardness", 0.0, 100.0, 1, static b => b.GlareHardness, static (b, v) => b.GlareHardness = v),
        new("GlareFactor", "Glare intensity", 0.0, 120.0, 1, static b => b.GlareFactor, static (b, v) => b.GlareFactor = v),
        new("GlareConvergence", "Glare convergence", 0.0, 100.0, 1, static b => b.GlareConvergence, static (b, v) => b.GlareConvergence = v),
        new("GlareOppositeFactor", "Opposite glare", 0.0, 100.0, 1, static b => b.GlareOppositeFactor, static (b, v) => b.GlareOppositeFactor = v),
        new("GlareAngle", "Glare angle", -180.0, 180.0, 1, static b => b.GlareAngle, static (b, v) => b.GlareAngle = v),
        new("Magnification", "Magnification", 1.0, 3.0, 0.05, static b => b.Magnification, static (b, v) => b.Magnification = v),
        new("MagnificationStrength", "Magnification strength", 0.0, 64.0, 0.5, static b => b.MagnificationStrength, static (b, v) => b.MagnificationStrength = v),
        new("BloomAmount", "Bloom", 0.0, 1.0, 0.05, static b => b.BloomAmount, static (b, v) => b.BloomAmount = v),
        new("Brightness", "Brightness", -1.0, 1.0, 0.05, static b => b.Brightness, static (b, v) => b.Brightness = v),
        new("Temperature", "Temperature", -1.0, 1.0, 0.05, static b => b.Temperature, static (b, v) => b.Temperature = v),
        new("Vibrance", "Vibrance", 0.0, 1.0, 0.05, static b => b.Vibrance, static (b, v) => b.Vibrance = v),
    ];

    public static double[] Capture(LiquidGlassBrush brush) => Descriptors.Select(d => d.Read(brush)).ToArray();

    public static LiquidGlassBrush Defaults(string kind) => LiquidGlassPresets.CreateBrush(kind switch
    {
        "Card" => LiquidGlassPreset.Panel,
        "FloatingPanel" => LiquidGlassPreset.FloatingPanel,
        "TabBar" => LiquidGlassPreset.TabBar,
        "SearchBox" => LiquidGlassPreset.SearchBox,
        "TextBox" or "PasswordBox" => LiquidGlassPreset.Input,
        "ComboBox" or "CheckBox" or "RadioButton" => LiquidGlassPreset.Choice,
        "Slider" => LiquidGlassPreset.SliderThumb,
        "ToggleSwitch" => LiquidGlassPreset.ToggleSwitchKnob,
        _ => LiquidGlassPreset.Button,
    });

    public static string Text(string key, string fallback) =>
        SH.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;
}
