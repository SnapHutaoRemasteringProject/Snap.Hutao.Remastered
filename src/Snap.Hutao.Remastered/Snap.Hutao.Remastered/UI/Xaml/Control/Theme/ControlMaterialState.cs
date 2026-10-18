// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.Xaml.Interactivity;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Core.Property;
using WinUI.Composition.Hlsl;
using WinUI.LiquidGlass;
using System.Runtime.CompilerServices;
using XamlControl = Microsoft.UI.Xaml.Controls.Control;
using XamlAutoSuggestBox = Microsoft.UI.Xaml.Controls.AutoSuggestBox;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

/// <summary>Persisted glass options and material selection, independent of a control's DataContext.</summary>
[BindableCustomPropertyProvider]
public sealed partial class ControlMaterialState : DependencyObject
{
    // The binding generator guards setters with this flag. This settings resource
    // is not owned by a page, so unloading a view must not disable its bindings.
    public IProperty<bool> IsViewUnloaded { get; } = Property.Create(false);

    private static readonly List<WeakReference<ControlMaterialState>> Instances = [];
    private static readonly Dictionary<FrameworkElement, BrushDefaults> ManagedControls = [];
    private static readonly ConditionalWeakTable<LiquidGlassBrush, BrushDefaults> SurfaceDefaults = new();
    private static ControlMaterial currentMaterial;
    private bool isSynchronizing = true;

    public ControlMaterialState()
    {
        ButtonEnabled = LocalSetting.Get("LiquidGlass.ButtonEnabled", true);
        ToggleButtonEnabled = LocalSetting.Get("LiquidGlass.ToggleButtonEnabled", true);
        HyperlinkButtonEnabled = LocalSetting.Get("LiquidGlass.HyperlinkButtonEnabled", true);
        CheckBoxEnabled = LocalSetting.Get("LiquidGlass.CheckBoxEnabled", true);
        RadioButtonEnabled = LocalSetting.Get("LiquidGlass.RadioButtonEnabled", true);
        SliderEnabled = LocalSetting.Get("LiquidGlass.SliderEnabled", true);
        TextBoxEnabled = LocalSetting.Get("LiquidGlass.TextBoxEnabled", true);
        SearchBoxEnabled = LocalSetting.Get("LiquidGlass.SearchBoxEnabled", true);
        PasswordBoxEnabled = LocalSetting.Get("LiquidGlass.PasswordBoxEnabled", true);
        ComboBoxEnabled = LocalSetting.Get("LiquidGlass.ComboBoxEnabled", true);
        ToggleSwitchEnabled = LocalSetting.Get("LiquidGlass.ToggleSwitchEnabled", true);
        CardEnabled = LocalSetting.Get("LiquidGlass.CardEnabled", true);
        FloatingPanelEnabled = LocalSetting.Get("LiquidGlass.FloatingPanelEnabled", true);
        TabBarEnabled = LocalSetting.Get("LiquidGlass.TabBarEnabled", true);
        Strength = Math.Clamp(LocalSetting.Get("LiquidGlass.Strength", 1.0), 0.0, 2.0);
        BlurScale = Math.Clamp(LocalSetting.Get("LiquidGlass.BlurScale", 1.0), 0.0, 2.0);
        RefractionScale = Math.Clamp(LocalSetting.Get("LiquidGlass.RefractionScale", 1.0), 0.0, 2.0);
        DispersionScale = Math.Clamp(LocalSetting.Get("LiquidGlass.DispersionScale", 1.0), 0.0, 2.0);
        HighlightScale = Math.Clamp(LocalSetting.Get("LiquidGlass.HighlightScale", 1.0), 0.0, 2.0);
        MagnificationScale = Math.Clamp(LocalSetting.Get("LiquidGlass.MagnificationScale", 1.0), 0.0, 2.0);
        Instances.Add(new(this));
        DispersionRange = LocalSetting.Get("LiquidGlass.DispersionRange", 1.0);
        RefFresnelRange = LocalSetting.Get("LiquidGlass.RefFresnelRange", 30.0);
        RefFresnelHardness = LocalSetting.Get("LiquidGlass.RefFresnelHardness", 20.0);
        RefFresnelFactor = LocalSetting.Get("LiquidGlass.RefFresnelFactor", 0.0);
        GlareRange = LocalSetting.Get("LiquidGlass.GlareRange", 30.0);
        GlareHardness = LocalSetting.Get("LiquidGlass.GlareHardness", 20.0);
        GlareFactor = LocalSetting.Get("LiquidGlass.GlareFactor", 0.0);
        GlareConvergence = LocalSetting.Get("LiquidGlass.GlareConvergence", 50.0);
        GlareOppositeFactor = LocalSetting.Get("LiquidGlass.GlareOppositeFactor", 80.0);
        GlareAngle = LocalSetting.Get("LiquidGlass.GlareAngle", -45.0);
        Magnification = LocalSetting.Get("LiquidGlass.Magnification", 1.0);
        BloomAmount = LocalSetting.Get("LiquidGlass.BloomAmount", 1.0);
        Brightness = LocalSetting.Get("LiquidGlass.Brightness", 0.0);
        Temperature = LocalSetting.Get("LiquidGlass.Temperature", 0.0);
        Vibrance = LocalSetting.Get("LiquidGlass.Vibrance", 0.0);
        isSynchronizing = false;
        UpdateSelection();
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int Value { get => (int)GetValue(ValueProperty); private set => SetValue(ValueProperty, value); }

    internal bool GetTypeEnabled(string kind) => kind switch
    {
        "Button" => ButtonEnabled,
        "ToggleButton" => ToggleButtonEnabled,
        "HyperlinkButton" => HyperlinkButtonEnabled,
        "CheckBox" => CheckBoxEnabled,
        "RadioButton" => RadioButtonEnabled,
        "Slider" => SliderEnabled,
        "TextBox" => TextBoxEnabled,
        "SearchBox" => SearchBoxEnabled,
        "PasswordBox" => PasswordBoxEnabled,
        "ComboBox" => ComboBoxEnabled,
        "ToggleSwitch" => ToggleSwitchEnabled,
        "Card" => CardEnabled,
        "FloatingPanel" => FloatingPanelEnabled,
        "TabBar" => TabBarEnabled,
        _ => false,
    };

    internal void SetTypeEnabled(string kind, bool value)
    {
        switch (kind)
        {
            case "Button": ButtonEnabled = value; break;
            case "ToggleButton": ToggleButtonEnabled = value; break;
            case "HyperlinkButton": HyperlinkButtonEnabled = value; break;
            case "CheckBox": CheckBoxEnabled = value; break;
            case "RadioButton": RadioButtonEnabled = value; break;
            case "Slider": SliderEnabled = value; break;
            case "TextBox": TextBoxEnabled = value; break;
            case "SearchBox": SearchBoxEnabled = value; break;
            case "PasswordBox": PasswordBoxEnabled = value; break;
            case "ComboBox": ComboBoxEnabled = value; break;
            case "ToggleSwitch": ToggleSwitchEnabled = value; break;
            case "Card": CardEnabled = value; break;
            case "FloatingPanel": FloatingPanelEnabled = value; break;
            case "TabBar": TabBarEnabled = value; break;
        }
    }

    public static readonly DependencyProperty ProfileKindProperty = DependencyProperty.RegisterAttached(
        "ProfileKind", typeof(string), typeof(ControlMaterialState), new PropertyMetadata(string.Empty));
    public static string GetProfileKind(DependencyObject target) => (string)target.GetValue(ProfileKindProperty);
    public static void SetProfileKind(DependencyObject target, string value) => target.SetValue(ProfileKindProperty, value);

    public static readonly DependencyProperty ButtonEnabledProperty = DependencyProperty.Register(
        nameof(ButtonEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool ButtonEnabled { get => (bool)GetValue(ButtonEnabledProperty); set => SetValue(ButtonEnabledProperty, value); }

    public static readonly DependencyProperty ButtonMaterialProperty = DependencyProperty.Register(
        nameof(ButtonMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int ButtonMaterial { get => (int)GetValue(ButtonMaterialProperty); private set => SetValue(ButtonMaterialProperty, value); }

    public static readonly DependencyProperty ToggleButtonEnabledProperty = DependencyProperty.Register(
        nameof(ToggleButtonEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool ToggleButtonEnabled { get => (bool)GetValue(ToggleButtonEnabledProperty); set => SetValue(ToggleButtonEnabledProperty, value); }

    public static readonly DependencyProperty ToggleButtonMaterialProperty = DependencyProperty.Register(
        nameof(ToggleButtonMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int ToggleButtonMaterial { get => (int)GetValue(ToggleButtonMaterialProperty); private set => SetValue(ToggleButtonMaterialProperty, value); }

    public static readonly DependencyProperty HyperlinkButtonEnabledProperty = DependencyProperty.Register(
        nameof(HyperlinkButtonEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool HyperlinkButtonEnabled { get => (bool)GetValue(HyperlinkButtonEnabledProperty); set => SetValue(HyperlinkButtonEnabledProperty, value); }

    public static readonly DependencyProperty HyperlinkButtonMaterialProperty = DependencyProperty.Register(
        nameof(HyperlinkButtonMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int HyperlinkButtonMaterial { get => (int)GetValue(HyperlinkButtonMaterialProperty); private set => SetValue(HyperlinkButtonMaterialProperty, value); }

    public static readonly DependencyProperty CheckBoxEnabledProperty = DependencyProperty.Register(
        nameof(CheckBoxEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool CheckBoxEnabled { get => (bool)GetValue(CheckBoxEnabledProperty); set => SetValue(CheckBoxEnabledProperty, value); }

    public static readonly DependencyProperty CheckBoxMaterialProperty = DependencyProperty.Register(
        nameof(CheckBoxMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int CheckBoxMaterial { get => (int)GetValue(CheckBoxMaterialProperty); private set => SetValue(CheckBoxMaterialProperty, value); }

    public static readonly DependencyProperty RadioButtonEnabledProperty = DependencyProperty.Register(
        nameof(RadioButtonEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool RadioButtonEnabled { get => (bool)GetValue(RadioButtonEnabledProperty); set => SetValue(RadioButtonEnabledProperty, value); }

    public static readonly DependencyProperty RadioButtonMaterialProperty = DependencyProperty.Register(
        nameof(RadioButtonMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int RadioButtonMaterial { get => (int)GetValue(RadioButtonMaterialProperty); private set => SetValue(RadioButtonMaterialProperty, value); }

    public static readonly DependencyProperty SliderEnabledProperty = DependencyProperty.Register(
        nameof(SliderEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool SliderEnabled { get => (bool)GetValue(SliderEnabledProperty); set => SetValue(SliderEnabledProperty, value); }

    public static readonly DependencyProperty SliderMaterialProperty = DependencyProperty.Register(
        nameof(SliderMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int SliderMaterial { get => (int)GetValue(SliderMaterialProperty); private set => SetValue(SliderMaterialProperty, value); }

    public static readonly DependencyProperty TextBoxEnabledProperty = DependencyProperty.Register(
        nameof(TextBoxEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool TextBoxEnabled { get => (bool)GetValue(TextBoxEnabledProperty); set => SetValue(TextBoxEnabledProperty, value); }

    public static readonly DependencyProperty SearchBoxEnabledProperty = DependencyProperty.Register(
        nameof(SearchBoxEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool SearchBoxEnabled { get => (bool)GetValue(SearchBoxEnabledProperty); set => SetValue(SearchBoxEnabledProperty, value); }
    public static readonly DependencyProperty SearchBoxMaterialProperty = DependencyProperty.Register(
        nameof(SearchBoxMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int SearchBoxMaterial { get => (int)GetValue(SearchBoxMaterialProperty); private set => SetValue(SearchBoxMaterialProperty, value); }

    public static readonly DependencyProperty TextBoxMaterialProperty = DependencyProperty.Register(
        nameof(TextBoxMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int TextBoxMaterial { get => (int)GetValue(TextBoxMaterialProperty); private set => SetValue(TextBoxMaterialProperty, value); }

    public static readonly DependencyProperty PasswordBoxEnabledProperty = DependencyProperty.Register(
        nameof(PasswordBoxEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool PasswordBoxEnabled { get => (bool)GetValue(PasswordBoxEnabledProperty); set => SetValue(PasswordBoxEnabledProperty, value); }

    public static readonly DependencyProperty PasswordBoxMaterialProperty = DependencyProperty.Register(
        nameof(PasswordBoxMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int PasswordBoxMaterial { get => (int)GetValue(PasswordBoxMaterialProperty); private set => SetValue(PasswordBoxMaterialProperty, value); }

    public static readonly DependencyProperty ComboBoxEnabledProperty = DependencyProperty.Register(
        nameof(ComboBoxEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool ComboBoxEnabled { get => (bool)GetValue(ComboBoxEnabledProperty); set => SetValue(ComboBoxEnabledProperty, value); }

    public static readonly DependencyProperty ComboBoxMaterialProperty = DependencyProperty.Register(
        nameof(ComboBoxMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int ComboBoxMaterial { get => (int)GetValue(ComboBoxMaterialProperty); private set => SetValue(ComboBoxMaterialProperty, value); }

    public static readonly DependencyProperty ToggleSwitchEnabledProperty = DependencyProperty.Register(
        nameof(ToggleSwitchEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool ToggleSwitchEnabled { get => (bool)GetValue(ToggleSwitchEnabledProperty); set => SetValue(ToggleSwitchEnabledProperty, value); }

    public static readonly DependencyProperty ToggleSwitchMaterialProperty = DependencyProperty.Register(
        nameof(ToggleSwitchMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int ToggleSwitchMaterial { get => (int)GetValue(ToggleSwitchMaterialProperty); private set => SetValue(ToggleSwitchMaterialProperty, value); }

    public static readonly DependencyProperty CardEnabledProperty = DependencyProperty.Register(
        nameof(CardEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool CardEnabled { get => (bool)GetValue(CardEnabledProperty); set => SetValue(CardEnabledProperty, value); }

    public static readonly DependencyProperty CardMaterialProperty = DependencyProperty.Register(
        nameof(CardMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int CardMaterial { get => (int)GetValue(CardMaterialProperty); private set => SetValue(CardMaterialProperty, value); }

    public static readonly DependencyProperty FloatingPanelEnabledProperty = DependencyProperty.Register(
        nameof(FloatingPanelEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool FloatingPanelEnabled { get => (bool)GetValue(FloatingPanelEnabledProperty); set => SetValue(FloatingPanelEnabledProperty, value); }

    public static readonly DependencyProperty FloatingPanelMaterialProperty = DependencyProperty.Register(
        nameof(FloatingPanelMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int FloatingPanelMaterial { get => (int)GetValue(FloatingPanelMaterialProperty); private set => SetValue(FloatingPanelMaterialProperty, value); }

    public static readonly DependencyProperty TabBarEnabledProperty = DependencyProperty.Register(
        nameof(TabBarEnabled), typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(true, OnOptionChanged));
    public bool TabBarEnabled { get => (bool)GetValue(TabBarEnabledProperty); set => SetValue(TabBarEnabledProperty, value); }

    public static readonly DependencyProperty TabBarMaterialProperty = DependencyProperty.Register(
        nameof(TabBarMaterial), typeof(int), typeof(ControlMaterialState), new PropertyMetadata(0));
    public int TabBarMaterial { get => (int)GetValue(TabBarMaterialProperty); private set => SetValue(TabBarMaterialProperty, value); }

    public static readonly DependencyProperty StrengthProperty = DependencyProperty.Register(
        nameof(Strength), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0, OnOptionChanged));
    public double Strength { get => (double)GetValue(StrengthProperty); set => SetValue(StrengthProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 2.0) : 1.0); }

    public static readonly DependencyProperty BlurScaleProperty = DependencyProperty.Register(
        nameof(BlurScale), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0, OnOptionChanged));
    public double BlurScale { get => (double)GetValue(BlurScaleProperty); set => SetValue(BlurScaleProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 2.0) : 1.0); }

    public static readonly DependencyProperty RefractionScaleProperty = DependencyProperty.Register(
        nameof(RefractionScale), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0, OnOptionChanged));
    public double RefractionScale { get => (double)GetValue(RefractionScaleProperty); set => SetValue(RefractionScaleProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 2.0) : 1.0); }

    public static readonly DependencyProperty DispersionScaleProperty = DependencyProperty.Register(
        nameof(DispersionScale), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0, OnOptionChanged));
    public double DispersionScale { get => (double)GetValue(DispersionScaleProperty); set => SetValue(DispersionScaleProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 2.0) : 1.0); }

    public static readonly DependencyProperty HighlightScaleProperty = DependencyProperty.Register(
        nameof(HighlightScale), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0, OnOptionChanged));
    public double HighlightScale { get => (double)GetValue(HighlightScaleProperty); set => SetValue(HighlightScaleProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 2.0) : 1.0); }

    public static readonly DependencyProperty MagnificationScaleProperty = DependencyProperty.Register(
        nameof(MagnificationScale), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0, OnOptionChanged));
    public double MagnificationScale { get => (double)GetValue(MagnificationScaleProperty); set => SetValue(MagnificationScaleProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 2.0) : 1.0); }

    internal static void Apply(ControlMaterial material)
    {
        bool changed = currentMaterial != material;
        if (changed) ComboBoxPopupLifetime.CloseAll();
        currentMaterial = material;
        ForEachState(state => state.UpdateSelection());
        if (changed) LiquidGlassProfiles.NotifySelectionChanged();
    }

    public static readonly DependencyProperty DispersionRangeProperty = DependencyProperty.Register(
        nameof(DispersionRange), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0, OnOptionChanged));
    public double DispersionRange
    {
        get => (double)GetValue(DispersionRangeProperty);
        set => SetValue(DispersionRangeProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : 1.0);
    }

    public static readonly DependencyProperty RefFresnelRangeProperty = DependencyProperty.Register(
        nameof(RefFresnelRange), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(30.0, OnOptionChanged));
    public double RefFresnelRange
    {
        get => (double)GetValue(RefFresnelRangeProperty);
        set => SetValue(RefFresnelRangeProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 100.0) : 30.0);
    }

    public static readonly DependencyProperty RefFresnelHardnessProperty = DependencyProperty.Register(
        nameof(RefFresnelHardness), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(20.0, OnOptionChanged));
    public double RefFresnelHardness
    {
        get => (double)GetValue(RefFresnelHardnessProperty);
        set => SetValue(RefFresnelHardnessProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 100.0) : 20.0);
    }

    public static readonly DependencyProperty RefFresnelFactorProperty = DependencyProperty.Register(
        nameof(RefFresnelFactor), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(0.0, OnOptionChanged));
    public double RefFresnelFactor
    {
        get => (double)GetValue(RefFresnelFactorProperty);
        set => SetValue(RefFresnelFactorProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 100.0) : 0.0);
    }

    public static readonly DependencyProperty GlareRangeProperty = DependencyProperty.Register(
        nameof(GlareRange), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(30.0, OnOptionChanged));
    public double GlareRange
    {
        get => (double)GetValue(GlareRangeProperty);
        set => SetValue(GlareRangeProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 100.0) : 30.0);
    }

    public static readonly DependencyProperty GlareHardnessProperty = DependencyProperty.Register(
        nameof(GlareHardness), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(20.0, OnOptionChanged));
    public double GlareHardness
    {
        get => (double)GetValue(GlareHardnessProperty);
        set => SetValue(GlareHardnessProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 100.0) : 20.0);
    }

    public static readonly DependencyProperty GlareFactorProperty = DependencyProperty.Register(
        nameof(GlareFactor), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(0.0, OnOptionChanged));
    public double GlareFactor
    {
        get => (double)GetValue(GlareFactorProperty);
        set => SetValue(GlareFactorProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 120.0) : 0.0);
    }

    public static readonly DependencyProperty GlareConvergenceProperty = DependencyProperty.Register(
        nameof(GlareConvergence), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(50.0, OnOptionChanged));
    public double GlareConvergence
    {
        get => (double)GetValue(GlareConvergenceProperty);
        set => SetValue(GlareConvergenceProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 100.0) : 50.0);
    }

    public static readonly DependencyProperty GlareOppositeFactorProperty = DependencyProperty.Register(
        nameof(GlareOppositeFactor), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(80.0, OnOptionChanged));
    public double GlareOppositeFactor
    {
        get => (double)GetValue(GlareOppositeFactorProperty);
        set => SetValue(GlareOppositeFactorProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 100.0) : 80.0);
    }

    public static readonly DependencyProperty GlareAngleProperty = DependencyProperty.Register(
        nameof(GlareAngle), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(-45.0, OnOptionChanged));
    public double GlareAngle
    {
        get => (double)GetValue(GlareAngleProperty);
        set => SetValue(GlareAngleProperty, double.IsFinite(value) ? Math.Clamp(value, -180.0, 180.0) : -45.0);
    }

    public static readonly DependencyProperty MagnificationProperty = DependencyProperty.Register(
        nameof(Magnification), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0, OnOptionChanged));
    public double Magnification
    {
        get => (double)GetValue(MagnificationProperty);
        set => SetValue(MagnificationProperty, double.IsFinite(value) ? Math.Clamp(value, 1.0, 3.0) : 1.0);
    }

    public static readonly DependencyProperty BloomAmountProperty = DependencyProperty.Register(
        nameof(BloomAmount), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0, OnOptionChanged));
    public double BloomAmount
    {
        get => (double)GetValue(BloomAmountProperty);
        set => SetValue(BloomAmountProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : 1.0);
    }

    public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register(
        nameof(Brightness), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(0.0, OnOptionChanged));
    public double Brightness
    {
        get => (double)GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, double.IsFinite(value) ? Math.Clamp(value, -1.0, 1.0) : 0.0);
    }

    public static readonly DependencyProperty TemperatureProperty = DependencyProperty.Register(
        nameof(Temperature), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(0.0, OnOptionChanged));
    public double Temperature
    {
        get => (double)GetValue(TemperatureProperty);
        set => SetValue(TemperatureProperty, double.IsFinite(value) ? Math.Clamp(value, -1.0, 1.0) : 0.0);
    }

    public static readonly DependencyProperty VibranceProperty = DependencyProperty.Register(
        nameof(Vibrance), typeof(double), typeof(ControlMaterialState), new PropertyMetadata(0.0, OnOptionChanged));
    public double Vibrance
    {
        get => (double)GetValue(VibranceProperty);
        set => SetValue(VibranceProperty, double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : 0.0);
    }

    public static FrameworkElement CreateButtonPresenter(Button native, RoutedEventHandler onClick)
    {
        LiquidGlassButton glass = new();
        SetIsManaged(glass, true);
        SetNativeControl(glass, native);
        glass.Click += onClick;
        // Each material resolves its own theme brushes, even while its peer is detached.
        foreach ((DependencyProperty property, string path) in new[]
        {
            (ContentControl.ContentProperty, nameof(ContentControl.Content)),
            (XamlControl.IsEnabledProperty, nameof(XamlControl.IsEnabled)),
            (Button.CommandProperty, nameof(Button.Command)),
            (Button.CommandParameterProperty, nameof(Button.CommandParameter)),
        })
        {
            glass.SetBinding(property, new Binding { Source = native, Path = new PropertyPath(path), Mode = BindingMode.OneWay });
        }
        SwitchPresenter presenter = new()
        {
            IsTabStop = false,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };
        presenter.SwitchCases.Add(new Case { Value = 0, Content = native });
        presenter.SwitchCases.Add(new Case { Value = 1, Content = glass });
        ThemeSynchronization.SetSource(native, presenter);
        ControlMaterialState? settings = null;
        ForEachState(state => settings ??= state);
        if (settings is not null)
        {
            presenter.SetBinding(SwitchPresenter.ValueProperty, new Binding
            {
                Source = settings,
                Path = new PropertyPath(nameof(ButtonMaterial)),
                Mode = BindingMode.OneWay,
            });
        }
        return presenter;
    }

    private void UpdateSelection()
    {
        Value = (int)currentMaterial;
        ButtonMaterial = currentMaterial is ControlMaterial.LiquidGlass && ButtonEnabled ? 1 : 0;
        ToggleButtonMaterial = currentMaterial is ControlMaterial.LiquidGlass && ToggleButtonEnabled ? 1 : 0;
        HyperlinkButtonMaterial = currentMaterial is ControlMaterial.LiquidGlass && HyperlinkButtonEnabled ? 1 : 0;
        CheckBoxMaterial = currentMaterial is ControlMaterial.LiquidGlass && CheckBoxEnabled ? 1 : 0;
        RadioButtonMaterial = currentMaterial is ControlMaterial.LiquidGlass && RadioButtonEnabled ? 1 : 0;
        SliderMaterial = currentMaterial is ControlMaterial.LiquidGlass && SliderEnabled ? 1 : 0;
        TextBoxMaterial = currentMaterial is ControlMaterial.LiquidGlass && TextBoxEnabled ? 1 : 0;
        SearchBoxMaterial = currentMaterial is ControlMaterial.LiquidGlass && SearchBoxEnabled ? 1 : 0;
        PasswordBoxMaterial = currentMaterial is ControlMaterial.LiquidGlass && PasswordBoxEnabled ? 1 : 0;
        ComboBoxMaterial = currentMaterial is ControlMaterial.LiquidGlass && ComboBoxEnabled ? 1 : 0;
        ToggleSwitchMaterial = currentMaterial is ControlMaterial.LiquidGlass && ToggleSwitchEnabled ? 1 : 0;
        CardMaterial = currentMaterial is ControlMaterial.LiquidGlass && CardEnabled ? 1 : 0;
        FloatingPanelMaterial = currentMaterial is ControlMaterial.LiquidGlass && FloatingPanelEnabled ? 1 : 0;
        TabBarMaterial = currentMaterial is ControlMaterial.LiquidGlass && TabBarEnabled ? 1 : 0;
    }

    private static void OnOptionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ControlMaterialState source = (ControlMaterialState)sender;
        if (source.isSynchronizing) return;
        if (args.Property == ComboBoxEnabledProperty) ComboBoxPopupLifetime.CloseAll();
        (double minimum, double maximum) = args.Property switch
        {
            var property when property == DispersionRangeProperty => (0.0, 1.0),
            var property when property == RefFresnelRangeProperty => (0.0, 100.0),
            var property when property == RefFresnelHardnessProperty => (0.0, 100.0),
            var property when property == RefFresnelFactorProperty => (0.0, 100.0),
            var property when property == GlareRangeProperty => (0.0, 100.0),
            var property when property == GlareHardnessProperty => (0.0, 100.0),
            var property when property == GlareFactorProperty => (0.0, 120.0),
            var property when property == GlareConvergenceProperty => (0.0, 100.0),
            var property when property == GlareOppositeFactorProperty => (0.0, 100.0),
            var property when property == GlareAngleProperty => (-180.0, 180.0),
            var property when property == MagnificationProperty => (1.0, 3.0),
            var property when property == BloomAmountProperty => (0.0, 1.0),
            var property when property == BrightnessProperty => (-1.0, 1.0),
            var property when property == TemperatureProperty => (-1.0, 1.0),
            var property when property == VibranceProperty => (0.0, 1.0),
            _ => (0.0, 2.0),
        };
        if (args.NewValue is double number && (!double.IsFinite(number) || number < minimum || number > maximum))
        {
            source.isSynchronizing = true;
            source.SetValue(args.Property, args.OldValue);
            source.isSynchronizing = false;
            return;
        }

        string name = args.Property == StrengthProperty ? nameof(Strength)
            : args.Property == BlurScaleProperty ? nameof(BlurScale)
            : args.Property == RefractionScaleProperty ? nameof(RefractionScale)
            : args.Property == DispersionScaleProperty ? nameof(DispersionScale)
            : args.Property == HighlightScaleProperty ? nameof(HighlightScale)
            : args.Property == MagnificationScaleProperty ? nameof(MagnificationScale)
            : args.Property == DispersionRangeProperty ? nameof(DispersionRange)
            : args.Property == RefFresnelRangeProperty ? nameof(RefFresnelRange)
            : args.Property == RefFresnelHardnessProperty ? nameof(RefFresnelHardness)
            : args.Property == RefFresnelFactorProperty ? nameof(RefFresnelFactor)
            : args.Property == GlareRangeProperty ? nameof(GlareRange)
            : args.Property == GlareHardnessProperty ? nameof(GlareHardness)
            : args.Property == GlareFactorProperty ? nameof(GlareFactor)
            : args.Property == GlareConvergenceProperty ? nameof(GlareConvergence)
            : args.Property == GlareOppositeFactorProperty ? nameof(GlareOppositeFactor)
            : args.Property == GlareAngleProperty ? nameof(GlareAngle)
            : args.Property == MagnificationProperty ? nameof(Magnification)
            : args.Property == BloomAmountProperty ? nameof(BloomAmount)
            : args.Property == BrightnessProperty ? nameof(Brightness)
            : args.Property == TemperatureProperty ? nameof(Temperature)
            : args.Property == VibranceProperty ? nameof(Vibrance)
            : args.Property == ButtonEnabledProperty ? nameof(ButtonEnabled)
            : args.Property == ToggleButtonEnabledProperty ? nameof(ToggleButtonEnabled)
            : args.Property == HyperlinkButtonEnabledProperty ? nameof(HyperlinkButtonEnabled)
            : args.Property == CheckBoxEnabledProperty ? nameof(CheckBoxEnabled)
            : args.Property == RadioButtonEnabledProperty ? nameof(RadioButtonEnabled)
            : args.Property == SliderEnabledProperty ? nameof(SliderEnabled)
            : args.Property == TextBoxEnabledProperty ? nameof(TextBoxEnabled)
            : args.Property == SearchBoxEnabledProperty ? nameof(SearchBoxEnabled)
            : args.Property == PasswordBoxEnabledProperty ? nameof(PasswordBoxEnabled)
            : args.Property == ComboBoxEnabledProperty ? nameof(ComboBoxEnabled)
            : args.Property == ToggleSwitchEnabledProperty ? nameof(ToggleSwitchEnabled)
            : args.Property == CardEnabledProperty ? nameof(CardEnabled)
            : args.Property == FloatingPanelEnabledProperty ? nameof(FloatingPanelEnabled)
            : args.Property == TabBarEnabledProperty ? nameof(TabBarEnabled)
            : throw new InvalidOperationException("Unknown liquid glass setting");
        if (args.NewValue is bool enabled) LocalSetting.Set("LiquidGlass." + name, enabled);
        else LocalSetting.Set("LiquidGlass." + name, (double)args.NewValue);

        ForEachState(state =>
        {
            state.isSynchronizing = true;
            state.SetValue(args.Property, args.NewValue);
            state.isSynchronizing = false;
            state.UpdateSelection();
        });
        LiquidGlassProfiles.NotifySelectionChanged();
        foreach (BrushDefaults defaults in ManagedControls.Values.ToArray()) ApplyOptics(defaults);
        MaterialTheme.Apply(currentMaterial);
    }

    private static void ForEachState(Action<ControlMaterialState> action)
    {
        for (int index = Instances.Count - 1; index >= 0; index--)
        {
            if (Instances[index].TryGetTarget(out ControlMaterialState? state)) action(state);
            else Instances.RemoveAt(index);
        }
    }

    public static readonly DependencyProperty SettingValueProperty = DependencyProperty.RegisterAttached(
        "SettingValue", typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0));
    public static double GetSettingValue(DependencyObject target) => (double)target.GetValue(SettingValueProperty);
    public static void SetSettingValue(DependencyObject target, double value) => target.SetValue(SettingValueProperty, value);

    public static readonly DependencyProperty SettingMinimumProperty = DependencyProperty.RegisterAttached(
        "SettingMinimum", typeof(double), typeof(ControlMaterialState), new PropertyMetadata(0.0));
    public static double GetSettingMinimum(DependencyObject target) => (double)target.GetValue(SettingMinimumProperty);
    public static void SetSettingMinimum(DependencyObject target, double value) => target.SetValue(SettingMinimumProperty, value);

    public static readonly DependencyProperty SettingMaximumProperty = DependencyProperty.RegisterAttached(
        "SettingMaximum", typeof(double), typeof(ControlMaterialState), new PropertyMetadata(2.0));
    public static double GetSettingMaximum(DependencyObject target) => (double)target.GetValue(SettingMaximumProperty);
    public static void SetSettingMaximum(DependencyObject target, double value) => target.SetValue(SettingMaximumProperty, value);

    public static readonly DependencyProperty SettingStepProperty = DependencyProperty.RegisterAttached(
        "SettingStep", typeof(double), typeof(ControlMaterialState), new PropertyMetadata(0.1));
    public static double GetSettingStep(DependencyObject target) => (double)target.GetValue(SettingStepProperty);
    public static void SetSettingStep(DependencyObject target, double value) => target.SetValue(SettingStepProperty, value);

    public static readonly DependencyProperty SettingLargeStepProperty = DependencyProperty.RegisterAttached(
        "SettingLargeStep", typeof(double), typeof(ControlMaterialState), new PropertyMetadata(1.0));
    public static double GetSettingLargeStep(DependencyObject target) => (double)target.GetValue(SettingLargeStepProperty);
    public static void SetSettingLargeStep(DependencyObject target, double value) => target.SetValue(SettingLargeStepProperty, value);

    public static readonly DependencyProperty SettingIsOnProperty = DependencyProperty.RegisterAttached(
        "SettingIsOn", typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(false));
    public static bool GetSettingIsOn(DependencyObject target) => (bool)target.GetValue(SettingIsOnProperty);
    public static void SetSettingIsOn(DependencyObject target, bool value) => target.SetValue(SettingIsOnProperty, value);

    public static readonly DependencyProperty IsManagedProperty = DependencyProperty.RegisterAttached(
        "IsManaged", typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(false, OnIsManagedChanged));
    public static bool GetIsManaged(DependencyObject target) => (bool)target.GetValue(IsManagedProperty);
    public static void SetIsManaged(DependencyObject target, bool value) => target.SetValue(IsManagedProperty, value);

    public static readonly DependencyProperty UsesCompiledEnabledBindingProperty = DependencyProperty.RegisterAttached(
        "UsesCompiledEnabledBinding", typeof(bool), typeof(ControlMaterialState), new PropertyMetadata(false));
    public static bool GetUsesCompiledEnabledBinding(DependencyObject target) => (bool)target.GetValue(UsesCompiledEnabledBindingProperty);
    public static void SetUsesCompiledEnabledBinding(DependencyObject target, bool value) => target.SetValue(UsesCompiledEnabledBindingProperty, value);

    public static readonly DependencyProperty NativeControlProperty = DependencyProperty.RegisterAttached(
        "NativeControl", typeof(DependencyObject), typeof(ControlMaterialState), new PropertyMetadata(null));
    public static DependencyObject? GetNativeControl(DependencyObject target) => (DependencyObject?)target.GetValue(NativeControlProperty);
    public static void SetNativeControl(DependencyObject target, DependencyObject value) => target.SetValue(NativeControlProperty, value);

    private static void OnIsManagedChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not FrameworkElement element) return;
        element.Loaded -= OnManagedLoaded;
        element.Unloaded -= OnManagedUnloaded;
        if ((bool)args.NewValue)
        {
            element.Loaded += OnManagedLoaded;
            element.Unloaded += OnManagedUnloaded;
            if (element.IsLoaded) OnManagedLoaded(element, new RoutedEventArgs());
        }
        else ManagedControls.Remove(element);
    }

    private static void OnManagedLoaded(object sender, RoutedEventArgs args)
    {
        if (XamlApplicationLifetime.Exiting) return;
        if (sender is not FrameworkElement element) return;
        // The inactive SwitchPresenter branch need not have been loaded. Resolve
        // its default style before copying layout DPs (notably ComboBox.Padding).
        if (GetNativeControl(element) is XamlControl nativeControl) nativeControl.ApplyTemplate();
        // IsEnabled belongs to the business control. Never write a transient
        // coerced false from an attaching glass visual back into its source.
        if (!GetUsesCompiledEnabledBinding(element) && GetNativeControl(element) is XamlControl enabledSource)
        {
            element.SetBinding(XamlControl.IsEnabledProperty, new Binding
            {
                Source = enabledSource,
                Path = new PropertyPath(nameof(XamlControl.IsEnabled)),
                Mode = BindingMode.OneWay,
            });
        }
        if (element is XamlControl control) control.ApplyTemplate();
        if (element is LiquidGlassSearchBox search && GetNativeControl(element) is XamlAutoSuggestBox nativeSearch)
        {
            MoveSearchBehaviors(nativeSearch, search.InnerAutoSuggestBox);
        }
        if (element is LiquidGlassPasswordBox password && GetNativeControl(element) is PasswordBox native)
        {
            password.InnerPasswordBox.Header = native.Header;
            password.InnerPasswordBox.HeaderTemplate = native.HeaderTemplate;
            password.InnerPasswordBox.PasswordRevealMode = native.PasswordRevealMode;
            password.InnerPasswordBox.MaxLength = native.MaxLength;
        }
        LiquidGlassBrush? brush = element switch
        {
            LiquidGlassSearchBox value => value.GlassBrush,
            LiquidGlassButton value => value.GlassBrush,
            LiquidGlassToggleButton value => value.GlassBrush,
            LiquidGlassHyperlinkButton value => value.GlassBrush,
            LiquidGlassCheckBox value => value.GlassBrush,
            LiquidGlassRadioButton value => value.GlassBrush,
            LiquidGlassSlider value => value.GlassBrush,
            LiquidGlassTextBox value => value.GlassBrush,
            LiquidGlassPasswordBox value => value.GlassBrush,
            LiquidGlassComboBox value => value.GlassBrush,
            LiquidGlassToggleSwitch value => value.GlassBrush,
            LiquidGlassCard value => value.GlassBrush,
            LiquidGlassFloatingPanel value => value.GlassBrush,
            LiquidGlassTabBar value => value.GlassBrush,
            _ => null,
        };
        if (brush is null) return;
        if (!ManagedControls.TryGetValue(element, out BrushDefaults? defaults) || defaults.Brush != brush)
        {
            defaults = SurfaceDefaults.GetValue(brush, value => new(
                LibraryKind(element), value, LiquidGlassOptics.Capture(value)));
            ManagedControls[element] = defaults;
        }
        ApplyOptics(defaults);
        ApplyControlSettings(element);
    }

    // Detach without writing composition properties while the brush is disconnecting.
    private static void OnManagedUnloaded(object sender, RoutedEventArgs args)
    {
        if (!XamlApplicationLifetime.Exiting && sender is LiquidGlassSearchBox search && GetNativeControl(search) is XamlAutoSuggestBox nativeSearch)
            MoveSearchBehaviors(search.InnerAutoSuggestBox, nativeSearch);
        if (sender is FrameworkElement element) ManagedControls.Remove(element);
    }

    // Rounded glass edges need an inset even while an inactive source control
    // still reports the dependency property's zero default. Preserve larger
    // caller-provided insets and the native vertical measurements.
    public static Thickness ComboBoxPadding(Thickness padding)
    {
        return new(Math.Max(12, padding.Left), padding.Top, Math.Max(4, padding.Right), padding.Bottom);
    }

    private static void MoveSearchBehaviors(DependencyObject source, DependencyObject target)
    {
        BehaviorCollection from = Interaction.GetBehaviors(source);
        BehaviorCollection to = Interaction.GetBehaviors(target);
        while (from.Count > 0)
        {
            DependencyObject behavior = from[0];
            from.RemoveAt(0);
            to.Add(behavior);
        }
    }

    private static string LibraryKind(FrameworkElement element)
    {
        string explicitKind = GetProfileKind(element);
        if (!string.IsNullOrEmpty(explicitKind)) return explicitKind;
        return element switch
        {
            LiquidGlassButton => "Button",
            LiquidGlassToggleButton => "ToggleButton",
            LiquidGlassHyperlinkButton => "HyperlinkButton",
            LiquidGlassCheckBox => "CheckBox",
            LiquidGlassRadioButton => "RadioButton",
            LiquidGlassSlider => "Slider",
            LiquidGlassTextBox => "TextBox",
            LiquidGlassSearchBox => "SearchBox",
            LiquidGlassPasswordBox => "PasswordBox",
            LiquidGlassComboBox => "ComboBox",
            LiquidGlassToggleSwitch => "ToggleSwitch",
            LiquidGlassCard => "Card",
            LiquidGlassFloatingPanel => "FloatingPanel",
            LiquidGlassTabBar => "TabBar",
            _ => "Card",
        };
    }

    private static void ApplyOptics(BrushDefaults defaults) =>
        LiquidGlassProfiles.Get(defaults.Kind).Apply(defaults.Brush, defaults.Values);

    private static void ApplyControlSettings(FrameworkElement element)
    {
        LiquidGlassProfile profile = LiquidGlassProfiles.Get(LibraryKind(element));
        LiquidGlassInteraction.SetPointerLightingEnabled(element, profile.PointerLightingEnabled);
        LiquidGlassInteraction.SetPointerLightInfluence(element, profile.PointerLightInfluence);
        if (element is LiquidGlassToggleSwitch toggleSwitch)
        {
            toggleSwitch.SwitchScale = profile.SwitchScale;
        }
    }

    internal static void RefreshManagedInteractions()
    {
        if (XamlApplicationLifetime.Exiting) return;
        foreach (FrameworkElement element in ManagedControls.Keys.ToArray())
            if (element.IsLoaded) ApplyControlSettings(element);
    }

    internal static void RefreshManagedOptics()
    {
        if (XamlApplicationLifetime.Exiting) return;
        foreach (KeyValuePair<FrameworkElement, BrushDefaults> item in ManagedControls.ToArray())
            if (item.Key.IsLoaded) ApplyOptics(item.Value);
        MaterialTheme.Apply(currentMaterial);
    }

    internal static void ConfigureSurface(LiquidGlassBrush brush)
    {
        BrushDefaults defaults = SurfaceDefaults.GetValue(brush, value =>
            new("Card", value, LiquidGlassOptics.Capture(LiquidGlassOptics.Defaults("Card"))));
        ApplyOptics(defaults);
    }

    private sealed record BrushDefaults(string Kind, LiquidGlassBrush Brush, double[] Values);
}

/// <summary>Selects control instances for strongly typed slots such as InfoBar.ActionButton.</summary>
public sealed partial class ControlMaterialSelector : IValueConverter
{
    public object? Native { get; set; }
    public object? Glass { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language) => (value is 1 ? Glass : Native)!;
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}


/// <summary>Typed access to the existing application settings resource for compiled bindings.</summary>
public static class MaterialBindingSource
{
    private static ControlMaterialState? current;

    public static string FormatSettingValue(double value) => value.ToString("0.00", System.Globalization.CultureInfo.CurrentCulture);

    public static ControlMaterialState Current
    {
        get
        {
            if (current is not null) return current;
            ResourceDictionary resources = Application.Current.Resources;
            if (resources.TryGetValue("ControlMaterialState", out object? value) && value is ControlMaterialState state)
                return current = state;
            foreach (ResourceDictionary dictionary in resources.MergedDictionaries)
            {
                if (dictionary.TryGetValue("ControlMaterialState", out value) && value is ControlMaterialState selection)
                    return current = selection;
            }
            throw new InvalidOperationException("ControlMaterialState resources must be initialized before compiled bindings.");
        }
    }
}
