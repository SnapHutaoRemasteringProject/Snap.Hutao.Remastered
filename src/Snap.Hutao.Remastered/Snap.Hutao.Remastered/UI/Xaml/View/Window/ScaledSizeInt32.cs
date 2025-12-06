// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Graphics;
using Snap.Hutao.Remastered.UI.Windowing;
using Windows.Graphics;

namespace Snap.Hutao.Remastered.UI.Xaml.View.Window;

internal static class ScaledSizeInt32
{
    public static SizeInt32 CreateForWindow(int width, int height, Microsoft.UI.Xaml.Window window)
    {
        return new SizeInt32(width, height).Scale(window.RasterizationScale);
    }
}