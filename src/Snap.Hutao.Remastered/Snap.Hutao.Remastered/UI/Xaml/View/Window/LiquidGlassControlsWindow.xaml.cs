// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.UI.Windowing.Abstraction;
using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using System.Collections.Immutable;
using Windows.Graphics;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public sealed partial class LiquidGlassControlsWindow : Microsoft.UI.Xaml.Window,
    IXamlWindowExtendContentIntoTitleBar, IXamlWindowHasInitSize, IXamlWindowClosedHandler
{
    private bool isReady;

    public LiquidGlassControlsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        IServiceScope scope = serviceProvider.CreateScope();
        this.InitializeController(scope.ServiceProvider);
        Title = LiquidGlassOptics.Text("LiquidGlassControlsWindowTitle", "Liquid glass controls");
        LiquidGlassProfiles.SelectionChanged += OnSelectionChanged;
        TypeSelector.SelectedIndex = 0;
        isReady = true;
    }

    public ControlMaterialState Settings => MaterialBindingSource.Current;
    public LiquidGlassProfile[] Profiles => LiquidGlassProfiles.All;
    public FrameworkElement TitleBarCaptionAccess => DragRegion;
    public ImmutableArray<FrameworkElement> TitleBarPassthrough => [];
    public SizeInt32 InitSize => ScaledSizeInt32.CreateForWindow(1000, 760, this);

    private void MaterialChanged(object sender, SelectionChangedEventArgs args)
    {
        if (isReady && sender is ComboBox { SelectedIndex: >= 0 } selector)
            LiquidGlassProfiles.SetMaterial(selector.SelectedIndex);
    }

    private void TypeChanged(object sender, SelectionChangedEventArgs args) => RefreshPreview();
    private void SelectorLoaded(object sender, RoutedEventArgs args)
    {
        if (TypeSelector.SelectedIndex < 0 && TypeSelector.Items.Count > 0) TypeSelector.SelectedIndex = 0;
        RefreshPreview();
    }
    private void OnSelectionChanged(object? sender, EventArgs args) => RefreshPreview();
    private void RefreshPreview()
    {
        if (PreviewHost is not null && TypeSelector?.SelectedItem is LiquidGlassProfile profile)
            PreviewHost.Child = LiquidGlassPreview.Create(profile, false);
    }

    public void OnWindowClosing(out bool cancel) => cancel = false;
    public void OnWindowClosed() => LiquidGlassProfiles.SelectionChanged -= OnSelectionChanged;
}
