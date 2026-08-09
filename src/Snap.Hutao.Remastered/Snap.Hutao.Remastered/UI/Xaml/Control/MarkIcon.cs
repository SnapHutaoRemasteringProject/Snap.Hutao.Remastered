// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

[DependencyProperty<bool>("IsMarked", DefaultValue = false, NotNull = true, PropertyChangedCallbackName = nameof(OnIsMarkedChanged))]
public sealed partial class MarkIcon : Microsoft.UI.Xaml.Controls.Control
{
    private const string IconUri = "ms-appx:///Resource/Icon/UI_Icon_UGC_Collect.png";

    private static readonly Windows.UI.Color GoldColor = Windows.UI.Color.FromArgb(102, 255, 200, 0);
    private static readonly Windows.UI.Color GrayColor = Windows.UI.Color.FromArgb(102, 128, 128, 128);

    private Microsoft.UI.Xaml.Controls.Image? iconImage;
    private SpriteVisual? overlayVisual;
    private Compositor? compositor;
    private CompositionColorBrush? overlayBrush;

    private static void OnIsMarkedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkIcon icon)
        {
            icon.UpdateOverlayColor();
        }
    }

    protected override void OnApplyTemplate()
    {
        if (GetTemplateChild("IconImage") is Microsoft.UI.Xaml.Controls.Image image)
        {
            iconImage = image;
            iconImage.Loaded += OnIconReady;
            compositor = ElementCompositionPreview.GetElementVisual(image).Compositor;
        }

        TryApplyOverlay();
    }

    private void OnIconReady(object sender, RoutedEventArgs e)
    {
        TryApplyOverlay();
    }

    private void UpdateOverlayColor()
    {
        if (overlayBrush is not null)
        {
            overlayBrush.Color = IsMarked ? GoldColor : GrayColor;
        }
        else
        {
            TryApplyOverlay();
        }
    }

    private void TryApplyOverlay()
    {
        if (overlayVisual is not null || iconImage is null || compositor is null)
        {
            return;
        }

        LoadedImageSurface surface = LoadedImageSurface.StartLoadFromUri(new System.Uri(IconUri));
        surface.LoadCompleted += (s, e) =>
        {
            if (e.Status is LoadedImageSourceLoadStatus.Success)
            {
                _ = iconImage.DispatcherQueue.TryEnqueue(() => ApplyOverlay(s));
            }
        };

        ApplyOverlay(surface);
    }

    private void ApplyOverlay(LoadedImageSurface surface)
    {
        if (compositor is null || iconImage is null)
        {
            return;
        }

        RemoveOverlay();

        CompositionSurfaceBrush surfaceBrush = compositor.CreateSurfaceBrush(surface);
        overlayBrush = compositor.CreateColorBrush(IsMarked ? GoldColor : GrayColor);
        CompositionMaskBrush maskBrush = compositor.CreateMaskBrush();
        maskBrush.Mask = surfaceBrush;
        maskBrush.Source = overlayBrush;

        Visual imageVisual = ElementCompositionPreview.GetElementVisual(iconImage);
        overlayVisual = compositor.CreateSpriteVisual();
        overlayVisual.Brush = maskBrush;

        ExpressionAnimation sizeAnimation = compositor.CreateExpressionAnimation("host.Size");
        sizeAnimation.SetReferenceParameter("host", imageVisual);
        overlayVisual.StartAnimation("Size", sizeAnimation);

        ElementCompositionPreview.SetElementChildVisual(iconImage, overlayVisual);
    }

    private void RemoveOverlay()
    {
        if (overlayVisual is not null && iconImage is not null)
        {
            ElementCompositionPreview.SetElementChildVisual(iconImage, null);
            overlayVisual.Dispose();
            overlayVisual = null;
            overlayBrush = null;
        }
    }
}
