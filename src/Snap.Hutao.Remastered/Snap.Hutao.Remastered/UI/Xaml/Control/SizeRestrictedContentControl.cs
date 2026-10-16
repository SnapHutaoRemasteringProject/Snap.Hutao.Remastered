// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Snap.Hutao.Remastered.UI.Xaml.Control;

[DependencyProperty<bool>("IsWidthRestricted", DefaultValue = true, NotNull = true)]
[DependencyProperty<bool>("IsHeightRestricted", DefaultValue = true, NotNull = true)]
public sealed partial class SizeRestrictedContentControl : ContentControl
{
    private double minContentWidth;
    private double minContentHeight;
    private double lastAvailableWidth = double.NaN;

    protected override Size MeasureOverride(Size availableSize)
    {
        // Wrapped content gets a new intrinsic height when its width changes.
        if (!lastAvailableWidth.Equals(availableSize.Width))
        {
            minContentHeight = 0;
            lastAvailableWidth = availableSize.Width;
        }

        Size desiredSize = base.MeasureOverride(availableSize);
        if (Content is not FrameworkElement element || element.Visibility is Visibility.Collapsed)
        {
            return desiredSize;
        }

        // Arrange may stretch a child to the entire viewport. That actual size
        // is not an intrinsic minimum, and must never be written back to it.
        return new(
            DesiredSizeRestriction.Apply(desiredSize.Width, availableSize.Width, IsWidthRestricted, ref minContentWidth),
            DesiredSizeRestriction.Apply(desiredSize.Height, availableSize.Height, IsHeightRestricted, ref minContentHeight));
    }
}
