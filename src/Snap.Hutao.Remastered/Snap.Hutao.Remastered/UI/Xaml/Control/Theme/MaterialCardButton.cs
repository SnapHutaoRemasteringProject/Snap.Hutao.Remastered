// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI.LiquidGlass;
using XamlControl = Microsoft.UI.Xaml.Controls.Control;

namespace Snap.Hutao.Remastered.UI.Xaml.Control.Theme;

/// <summary>A home card owns one surface and one body without a clipping UserControl host.</summary>
[Microsoft.UI.Xaml.Markup.ContentProperty(Name = nameof(Body))]
public partial class MaterialCardButton : ContentControl
{
    private readonly Button nativeButton = new();
    private readonly LiquidGlassButton glassButton = new();
    private ControlMaterialState? materialState;
    private long materialToken;

    public static readonly DependencyProperty BodyProperty = DependencyProperty.Register(
        nameof(Body), typeof(UIElement), typeof(MaterialCardButton), new PropertyMetadata(null, OnBodyChanged));
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(MaterialCardButton), new PropertyMetadata(null));
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(MaterialCardButton), new PropertyMetadata(null));

    public MaterialCardButton()
    {
        DefaultStyleKey = typeof(ContentControl);
        Template = (ControlTemplate)Application.Current.Resources["HomeCardHostTemplate"];
        IsTabStop = false;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        nativeButton.Style = (Style)Application.Current.Resources["NativeHomeCardButtonStyle"];
        glassButton.GlassBrush = LiquidGlassPresets.CreateBrush(LiquidGlassPreset.Panel);
        ControlMaterialState.SetProfileKind(glassButton, "Card");
        ControlMaterialState.SetIsManaged(glassButton, true);

        foreach (Button button in new Button[] { nativeButton, glassButton })
        {
            Mirror(button, Button.CommandProperty, CommandProperty);
            Mirror(button, Button.CommandParameterProperty, CommandParameterProperty);
            Mirror(button, XamlControl.PaddingProperty, XamlControl.PaddingProperty);
            Mirror(button, XamlControl.ForegroundProperty, XamlControl.ForegroundProperty);
            Mirror(button, XamlControl.HorizontalContentAlignmentProperty, XamlControl.HorizontalContentAlignmentProperty);
            Mirror(button, XamlControl.VerticalContentAlignmentProperty, XamlControl.VerticalContentAlignmentProperty);
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.VerticalAlignment = VerticalAlignment.Stretch;
        }

        WeakReference<MaterialCardButton> weak = new(this);
        nativeButton.RegisterPropertyChangedCallback(XamlControl.CornerRadiusProperty, (_, _) =>
        {
            if (weak.TryGetTarget(out MaterialCardButton? card)) card.SynchronizeSurfaceCorners();
        });
        SynchronizeSurfaceCorners();
        Content = nativeButton;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public UIElement? Body { get => (UIElement?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private void Mirror(Button target, DependencyProperty targetProperty, DependencyProperty sourceProperty)
    {
        // Dependency-property callbacks do not require reflection/custom providers.
        WeakReference<Button> weak = new(target);
        RegisterPropertyChangedCallback(sourceProperty, (sender, property) =>
        {
            if (weak.TryGetTarget(out Button? button)) button.SetValue(targetProperty, sender.GetValue(property));
        });
        target.SetValue(targetProperty, GetValue(sourceProperty));
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        DetachMaterialState();
        materialState = MaterialBindingSource.Current;
        WeakReference<MaterialCardButton> weak = new(this);
        materialToken = materialState.RegisterPropertyChangedCallback(ControlMaterialState.CardMaterialProperty, (_, _) =>
        {
            if (weak.TryGetTarget(out MaterialCardButton? card) && card.IsLoaded) card.MoveBody();
        });
        SynchronizeSurfaceCorners();
        MoveBody();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args) => DetachMaterialState();

    private void DetachMaterialState()
    {
        if (materialState is not null) materialState.UnregisterPropertyChangedCallback(ControlMaterialState.CardMaterialProperty, materialToken);
        materialState = null;
    }

    private void SynchronizeSurfaceCorners()
    {
        // The unclipped host exposes the surface radius to the dashboard's shadow.
        // Keep the shadow cutout, XAML contour and shader on the same geometry.
        CornerRadius corners = nativeButton.CornerRadius;
        CornerRadius = corners;
        glassButton.CornerRadius = corners;
        glassButton.GlassBrush.CornerRadius = corners.TopLeft;
    }

    private static void OnBodyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => ((MaterialCardButton)sender).MoveBody();

    private void MoveBody()
    {
        Button active = materialState?.CardMaterial is 1 ? glassButton : nativeButton;
        if (ReferenceEquals(Content, active) && ReferenceEquals(active.Content, Body)) return;
        nativeButton.Content = null;
        glassButton.Content = null;
        Content = null;
        active.Content = Body;
        Content = active;
    }
}
