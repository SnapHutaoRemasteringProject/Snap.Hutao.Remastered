// Copyright (c) Millennium-Science-Technology-R&D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

/// <summary>
/// Hosts a native control and its liquid glass counterpart for the material kinds whose
/// glass type cannot be restyled in place: the password and search boxes are
/// ContentControl wrappers, and the slider hides the native thumb and paints its own
/// lens in OnApplyTemplate. Everything else uses a single instance and
/// <see cref="MaterialAppearance"/>.
/// </summary>
public sealed partial class MaterialSwap : ContentControl
{
    public static readonly DependencyProperty NativeProperty = DependencyProperty.Register(
        nameof(Native), typeof(object), typeof(MaterialSwap), new PropertyMetadata(null, OnContentPropertyChanged));

    public static readonly DependencyProperty GlassProperty = DependencyProperty.Register(
        nameof(Glass), typeof(object), typeof(MaterialSwap), new PropertyMetadata(null, OnContentPropertyChanged));

    public MaterialSwap()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        IsTabStop = false;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public object? Native { get => GetValue(NativeProperty); set => SetValue(NativeProperty, value); }

    public object? Glass { get => GetValue(GlassProperty); set => SetValue(GlassProperty, value); }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateContent();
    }

    private static void OnContentPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((MaterialSwap)sender).UpdateContent();
    }

    private void OnMaterialChanged()
    {
        if (XamlApplicationLifetime.Exiting) return;
        UpdateContent();
    }

    private void UpdateContent()
    {
        object? content = UseGlass() ? Glass : Native;
        if (ReferenceEquals(Content, content)) return;
        Content = content;
    }

    private bool UseGlass()
    {
        if (Glass is not FrameworkElement glass) return false;
        if (!string.IsNullOrEmpty(ControlMaterialState.GetProfileKind(glass))) return true;
        return ControlMaterialState.IsGlassMaterial && MaterialBindingSource.Current.GetTypeEnabled(ControlMaterialState.LibraryKind(glass));
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        ControlMaterialState.MaterialChanged += OnMaterialChanged;
        UpdateContent();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        ControlMaterialState.MaterialChanged -= OnMaterialChanged;
    }
}