// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Extension;

internal static class SizeExtension
{
    public static double Size(this Windows.Foundation.Size size)
    {
        return size.Width * size.Height;
    }
}