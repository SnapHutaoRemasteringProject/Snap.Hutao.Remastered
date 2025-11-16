// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Windows.Graphics;

namespace Snap.Hutao.Core.Graphics;

internal static class SizeInt32Extension
{
    extension(SizeInt32 sizeInt32)
    {
        public int Size { get => sizeInt32.Width * sizeInt32.Height; }

        public SizeInt32 Scale(double scale)
        {
            return new((int)(sizeInt32.Width * scale), (int)(sizeInt32.Height * scale));
        }

        public unsafe RectInt32 ToRectInt32()
        {
            RectInt32View view = default;
            view.Size = sizeInt32;
            return *(RectInt32*)&view;
        }
    }
}