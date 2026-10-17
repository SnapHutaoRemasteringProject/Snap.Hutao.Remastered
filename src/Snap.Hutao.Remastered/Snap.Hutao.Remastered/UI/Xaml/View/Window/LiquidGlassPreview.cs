// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Snap.Hutao.Remastered.UI.Xaml.Control.Theme;
using Windows.UI;
using WinUI.LiquidGlass;
using XamlControl = Microsoft.UI.Xaml.Controls.Control;
using Shapes = Microsoft.UI.Xaml.Shapes;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

public static class LiquidGlassPreview
{
    public static UIElement Create(LiquidGlassProfile profile, bool forceGlass)
    {
        Grid scene = new() { MinHeight = 300 };
        LinearGradientBrush background = new() { StartPoint = new(0, 0), EndPoint = new(1, 1) };
        background.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 36, 82, 126), Offset = 0 });
        background.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 89, 41, 104), Offset = 1 });
        scene.Background = background;
        Canvas shapes = new() { IsHitTestVisible = false };
        Shapes.Ellipse cyan = new() { Width = 180, Height = 180, Fill = new SolidColorBrush(Color.FromArgb(255, 32, 185, 201)) };
        Canvas.SetLeft(cyan, 10); Canvas.SetTop(cyan, 45); shapes.Children.Add(cyan);
        Shapes.Ellipse pink = new() { Width = 140, Height = 140, Fill = new SolidColorBrush(Color.FromArgb(255, 232, 91, 142)) };
        Canvas.SetLeft(pink, 210); Canvas.SetTop(pink, 130); shapes.Children.Add(pink);
        Shapes.Rectangle stripe = new() { Width = 380, Height = 28, Fill = new SolidColorBrush(Color.FromArgb(255, 246, 199, 79)) };
        Canvas.SetTop(stripe, 130); shapes.Children.Add(stripe);
        scene.Children.Add(shapes);

        bool glass = forceGlass || profile.IsGlassActive;
        XamlControl sample;
        string buttonText = LiquidGlassOptics.Text("GlassPreviewButton", "Preview button");
        switch (profile.Key)
        {
            case "Button":
                Button button = glass ? new LiquidGlassButton() : new Button();
                button.Content = buttonText; sample = button; break;
            case "ToggleButton":
                ToggleButton toggle = glass ? new LiquidGlassToggleButton() : new ToggleButton();
                toggle.Content = buttonText; sample = toggle; break;
            case "HyperlinkButton":
                HyperlinkButton link = glass ? new LiquidGlassHyperlinkButton() : new HyperlinkButton();
                link.Content = buttonText; sample = link; break;
            case "CheckBox":
                CheckBox check = glass ? new LiquidGlassCheckBox() : new CheckBox();
                check.Content = buttonText; sample = check; break;
            case "RadioButton":
                RadioButton radio = glass ? new LiquidGlassRadioButton() : new RadioButton();
                radio.Content = buttonText; sample = radio; break;
            case "Slider":
                Slider slider = glass ? new LiquidGlassSlider() : new Slider();
                slider.Width = 270; slider.Minimum = 0; slider.Maximum = 100; slider.Value = 45; sample = slider; break;
            case "TextBox":
                TextBox text = glass ? new LiquidGlassTextBox() : new TextBox();
                text.Width = 260; text.PlaceholderText = LiquidGlassOptics.Text("GlassPreviewInput", "Type to preview focus");
                sample = text; break;
            case "ComboBox":
                ComboBox combo = glass ? new LiquidGlassComboBox() : new ComboBox();
                combo.Width = 240; combo.Items.Add(LiquidGlassOptics.Text("GlassPreviewFirst", "First"));
                combo.Items.Add(LiquidGlassOptics.Text("GlassPreviewSecond", "Second"));
                combo.SelectedIndex = 0; sample = combo; break;
            case "ToggleSwitch":
                if (glass) sample = new LiquidGlassToggleSwitch { IsOn = true };
                else sample = new ToggleSwitch { IsOn = true };
                break;
            case "SearchBox":
                string searchHint = LiquidGlassOptics.Text("GlassPreviewSearch", "Search preview");
                if (glass) sample = new LiquidGlassSearchBox { Width = 260, PlaceholderText = searchHint };
                else sample = new AutoSuggestBox { Width = 260, PlaceholderText = searchHint };
                break;
            case "PasswordBox":
                string passwordHint = LiquidGlassOptics.Text("GlassPreviewPassword", "Password preview");
                if (glass) sample = new LiquidGlassPasswordBox { Width = 260, PlaceholderText = passwordHint };
                else sample = new PasswordBox { Width = 260, PlaceholderText = passwordHint };
                break;
            case "TabBar":
                if (glass)
                {
                    LiquidGlassTabBar tabs = new() { Width = 330 };
                    tabs.Items.Add(LiquidGlassOptics.Text("GlassPreviewOverview", "Overview"));
                    tabs.Items.Add(LiquidGlassOptics.Text("GlassPreviewDetails", "Details"));
                    tabs.Items.Add(LiquidGlassOptics.Text("GlassPreviewActivity", "Activity"));
                    tabs.SelectedIndex = 0; sample = tabs;
                }
                else
                {
                    ComboBox tabs = new() { Width = 240 };
                    tabs.Items.Add(LiquidGlassOptics.Text("GlassPreviewOverview", "Overview"));
                    tabs.Items.Add(LiquidGlassOptics.Text("GlassPreviewDetails", "Details"));
                    tabs.Items.Add(LiquidGlassOptics.Text("GlassPreviewActivity", "Activity"));
                    tabs.SelectedIndex = 0; sample = tabs;
                }
                break;
            default:
                ContentControl card = new();
                if (glass && profile.Key == "Card") card = new LiquidGlassCard();
                if (glass && profile.Key == "FloatingPanel") card = new LiquidGlassFloatingPanel();
                card.Content = new TextBlock
                {
                    Text = LiquidGlassOptics.Text("GlassPreviewSurface", "Glass surface — content remains readable"),
                    TextWrapping = TextWrapping.Wrap,
                };
                card.Padding = new Thickness(24); card.MaxWidth = 330; sample = card; break;
        }

        sample.HorizontalAlignment = HorizontalAlignment.Center;
        sample.VerticalAlignment = VerticalAlignment.Center;
        if (glass)
        {
            ControlMaterialState.SetProfileKind(sample, profile.Key);
            ControlMaterialState.SetIsManaged(sample, true);
        }
        scene.Children.Add(sample);
        return scene;
    }
}
