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

public sealed partial class LiquidGlassEffectsWindow : Microsoft.UI.Xaml.Window,
    IXamlWindowExtendContentIntoTitleBar, IXamlWindowHasInitSize, INotifyPropertyChanged
{
    private LiquidGlassProfile selectedProfile = LiquidGlassProfiles.All[0];

    public LiquidGlassEffectsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        IServiceScope scope = serviceProvider.CreateScope();
        this.InitializeController(scope.ServiceProvider);
        Title = LiquidGlassOptics.Text("LiquidGlassEffectsWindowTitle", "Liquid glass effects");
        TypeSelector.SelectedIndex = 0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public LiquidGlassProfile[] Profiles => LiquidGlassProfiles.All;
    public LiquidGlassProfile SelectedProfile => selectedProfile;
    public FrameworkElement TitleBarCaptionAccess => DragRegion;
    public ImmutableArray<FrameworkElement> TitleBarPassthrough => [];
    public SizeInt32 InitSize => ScaledSizeInt32.CreateForWindow(1120, 820, this);

    private void TypeChanged(object sender, SelectionChangedEventArgs args)
    {
        if (TypeSelector?.SelectedItem is not LiquidGlassProfile profile || PreviewHost is null) return;
        selectedProfile = profile;
        PropertyChanged?.Invoke(this, new(nameof(SelectedProfile)));
        PreviewHost.Child = LiquidGlassPreview.Create(profile, true);
    }

    private void ResetClicked(object sender, RoutedEventArgs args) => SelectedProfile.Reset();

    private void SelectorLoaded(object sender, RoutedEventArgs args)
    {
        if (TypeSelector.SelectedIndex < 0 && TypeSelector.Items.Count > 0) TypeSelector.SelectedIndex = 0;
        if (PreviewHost.Child is null) PreviewHost.Child = LiquidGlassPreview.Create(SelectedProfile, true);
    }
}
