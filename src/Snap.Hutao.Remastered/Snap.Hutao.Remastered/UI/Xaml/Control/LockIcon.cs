// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

[DependencyProperty<bool>("IsLocked", DefaultValue = false, NotNull = true, PropertyChangedCallbackName = nameof(OnIsLockedChanged))]
public sealed partial class LockIcon : Microsoft.UI.Xaml.Controls.Control
{
    private const string LockedIconUri = "ms-appx:///Resource/Icon/UI_Icon_Locked.png";

    private Microsoft.UI.Xaml.Controls.Image? iconImage;
    private SpriteVisual? redOverlayVisual;
    private Compositor? compositor;

    private static void OnIsLockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LockIcon icon)
        {
            icon.UpdateVisualState();
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

        UpdateVisualState();
    }

    private void OnIconReady(object sender, RoutedEventArgs e)
    {
        if (IsLocked)
        {
            TryApplyRedOverlay();
        }
    }

    private void UpdateVisualState()
    {
        VisualStateManager.GoToState(this, IsLocked ? "Locked" : "Unlock", false);

        if (IsLocked)
        {
            TryApplyRedOverlay();
        }
        else
        {
            RemoveRedOverlay();
        }
    }

    private void TryApplyRedOverlay()
    {
        if (redOverlayVisual is not null || iconImage is null || compositor is null)
        {
            return;
        }

        LoadedImageSurface surface = LoadedImageSurface.StartLoadFromUri(new System.Uri(LockedIconUri));
        surface.LoadCompleted += (s, e) =>
        {
            if (e.Status is LoadedImageSourceLoadStatus.Success)
            {
                _ = iconImage.DispatcherQueue.TryEnqueue(() => ApplyRedOverlay(s));
            }
        };

        ApplyRedOverlay(surface);
    }

    private void ApplyRedOverlay(LoadedImageSurface surface)
    {
        if (compositor is null || iconImage is null)
        {
            return;
        }

        RemoveRedOverlay();

        CompositionSurfaceBrush surfaceBrush = compositor.CreateSurfaceBrush(surface);
        CompositionColorBrush redBrush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(102, 255, 0, 0));
        CompositionMaskBrush maskBrush = compositor.CreateMaskBrush();
        maskBrush.Mask = surfaceBrush;
        maskBrush.Source = redBrush;

        Visual imageVisual = ElementCompositionPreview.GetElementVisual(iconImage);
        redOverlayVisual = compositor.CreateSpriteVisual();
        redOverlayVisual.Brush = maskBrush;

        ExpressionAnimation sizeAnimation = compositor.CreateExpressionAnimation("host.Size");
        sizeAnimation.SetReferenceParameter("host", imageVisual);
        redOverlayVisual.StartAnimation("Size", sizeAnimation);

        ElementCompositionPreview.SetElementChildVisual(iconImage, redOverlayVisual);
    }

    private void RemoveRedOverlay()
    {
        if (redOverlayVisual is not null && iconImage is not null)
        {
            ElementCompositionPreview.SetElementChildVisual(iconImage, null);
            redOverlayVisual.Dispose();
            redOverlayVisual = null;
        }
    }
}
