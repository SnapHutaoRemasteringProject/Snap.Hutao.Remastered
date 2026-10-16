// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Core.Setting;
using Snap.Hutao.Remastered.Service;
using System.Globalization;
using WinUI.Composition.Hlsl;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

public sealed partial class LiquidGlassParameter : ObservableObject
{
    private readonly LiquidGlassProfile profile;
    private readonly LiquidGlassDescriptor descriptor;

    internal LiquidGlassParameter(LiquidGlassProfile profile, LiquidGlassDescriptor descriptor, double defaultValue)
    {
        this.profile = profile;
        this.descriptor = descriptor;
        DefaultValue = defaultValue;
    }

    public string Key => descriptor.Key;
    public string Label => LiquidGlassOptics.Text("GlassParameter" + Key, descriptor.EnglishLabel);
    public double DefaultValue { get; }
    public double Minimum => Math.Min(descriptor.Minimum, DefaultValue);
    public double Maximum => Math.Max(descriptor.Maximum, DefaultValue);
    public double Step => descriptor.Step;
    public double Value
    {
        get => profile.ReadOverride(Key);
        set
        {
            if (!double.IsFinite(value) || value < Minimum || value > Maximum || value == Value) return;
            profile.WriteOverride(this, value);
            Refresh();
        }
    }

    public string DisplayValue => Value.ToString("0.00", CultureInfo.CurrentCulture);
    public string DefaultText => DefaultValue.ToString("0.00", CultureInfo.CurrentCulture);
    public bool IsDefault => Value == DefaultValue;
    public void Reset() => Value = DefaultValue;

    internal void Refresh()
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(IsDefault));
    }
}

public sealed partial class LiquidGlassProfile : ObservableObject
{
    private readonly LiquidGlassOverrideStore overrides;
    private string StorageKey => "LiquidGlass.Profiles.v1." + Key;

    internal LiquidGlassProfile(string key)
    {
        Key = key;
        LiquidGlassBrush defaults = LiquidGlassOptics.Defaults(key);
        Parameters = LiquidGlassOptics.Descriptors.Select(d => new LiquidGlassParameter(this, d, d.Read(defaults))).ToArray();
        overrides = new(LocalSetting.Get(StorageKey, "{}"),
            Parameters.Select(static parameter => new LiquidGlassParameterRange(
                parameter.Key, parameter.Minimum, parameter.Maximum, parameter.DefaultValue)));
    }

    public string Key { get; }
    public string Name => LiquidGlassOptics.Text("GlassProfile" + Key, Key);
    public LiquidGlassParameter[] Parameters { get; }
    public bool Enabled
    {
        get => MaterialBindingSource.Current.GetTypeEnabled(Key);
        set
        {
            if (Enabled != value) MaterialBindingSource.Current.SetTypeEnabled(Key, value);
        }
    }

    public bool IsGlassActive => MaterialBindingSource.Current.Value == 1 && Enabled;
    internal double ReadOverride(string key) => overrides.Get(key);
    internal void WriteOverride(LiquidGlassParameter parameter, double value)
    {
        if (overrides.TrySet(parameter.Key, value)) Save();
    }

    private void Save()
    {
        LocalSetting.Set(StorageKey, overrides.Serialize());
        LiquidGlassProfiles.RefreshOptics();
    }

    public void Reset()
    {
        overrides.Reset();
        Save();
        foreach (LiquidGlassParameter parameter in Parameters) parameter.Refresh();
    }

    internal void Apply(LiquidGlassBrush brush, double[] defaults)
    {
        // Restore only optical slots. The actual library control still owns
        // geometry, corner radius, DPI and interaction animation.
        for (int index = 0; index < LiquidGlassOptics.Descriptors.Length; index++)
        {
            LiquidGlassDescriptor descriptor = LiquidGlassOptics.Descriptors[index];
            LiquidGlassParameter parameter = Parameters[index];
            double value = overrides.Get(descriptor.Key);
            descriptor.Write(brush, value == parameter.DefaultValue ? defaults[index] : value);
        }
    }

    internal void RefreshSelection()
    {
        OnPropertyChanged(nameof(Enabled));
        OnPropertyChanged(nameof(IsGlassActive));
    }
}

public static class LiquidGlassProfiles
{
    private static LiquidGlassProfile[]? profiles;
    private static bool refreshPending;
    public static LiquidGlassProfile[] All => profiles ??= [
        new("Button"),
        new("ToggleButton"),
        new("HyperlinkButton"),
        new("CheckBox"),
        new("RadioButton"),
        new("Slider"),
        new("TextBox"),
        new("SearchBox"),
        new("PasswordBox"),
        new("ComboBox"),
        new("ToggleSwitch"),
        new("Card"),
        new("FloatingPanel"),
        new("TabBar"),
    ];
    public static event EventHandler? SelectionChanged;

    public static void SetMaterial(int index)
    {
        if (index is not (0 or 1)) return;
        AppOptions options = Ioc.Default.GetRequiredService<AppOptions>();
        ControlMaterial value = (ControlMaterial)index;
        if (options.ControlMaterial.Value == value) return;
        options.ControlMaterial.Value = value;
        MaterialTheme.Apply(value);
    }

    internal static LiquidGlassProfile Get(string kind) => All.Single(profile => profile.Key == kind);
    internal static void NotifySelectionChanged()
    {
        if (profiles is not null)
            foreach (LiquidGlassProfile profile in profiles) profile.RefreshSelection();
        SelectionChanged?.Invoke(null, EventArgs.Empty);
    }

    internal static void RefreshOptics()
    {
        if (refreshPending || XamlApplicationLifetime.Exiting) return;
        refreshPending = true;
        if (!MaterialBindingSource.Current.DispatcherQueue.TryEnqueue(() =>
        {
            refreshPending = false;
            if (!XamlApplicationLifetime.Exiting) ControlMaterialState.RefreshManagedOptics();
        })) refreshPending = false;
    }
}
