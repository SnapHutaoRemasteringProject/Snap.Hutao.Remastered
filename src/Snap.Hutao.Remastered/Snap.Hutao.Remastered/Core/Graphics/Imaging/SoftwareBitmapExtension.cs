// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace Snap.Hutao.Remastered.Core.Graphics.Imaging;

public static class SoftwareBitmapExtension
{
    extension(SoftwareBitmap softwareBitmap)
    {
        public void NormalBlend(Bgra32 tint)
        {
            using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.ReadWrite))
            {
                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    Span<Bgra32> bytes = reference.GetBuffer<Bgra32>();
                    foreach (ref Bgra32 pixel in bytes)
                    {
                        byte baseAlpha = pixel.A;
                        int opposite = 0xFF - baseAlpha;
                        pixel.B = (byte)(((pixel.B * baseAlpha) + (tint.B * opposite)) / 0xFF);
                        pixel.G = (byte)(((pixel.G * baseAlpha) + (tint.G * opposite)) / 0xFF);
                        pixel.R = (byte)(((pixel.R * baseAlpha) + (tint.R * opposite)) / 0xFF);
                        pixel.A = 0xFF;
                    }
                }
            }
        }

        public Bgra32 GetBgra32AccentColor()
        {
            using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read))
            {
                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    Span<Bgra32> bytes = reference.GetBuffer<Bgra32>();
                    double b = 0, g = 0, r = 0, a = 0;
                    foreach (ref readonly Bgra32 pixel in bytes)
                    {
                        b += pixel.B;
                        g += pixel.G;
                        r += pixel.R;
                        a += pixel.A;
                    }

                    return new((byte)(b / bytes.Length), (byte)(g / bytes.Length), (byte)(r / bytes.Length), (byte)(a / bytes.Length));
                }
            }
        }

        public Rgba32 GetRgba32AccentColor()
        {
            using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read))
            {
                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    Span<Bgra32> bytes = reference.GetBuffer<Bgra32>();
                    double b = 0, g = 0, r = 0, a = 0;
                    foreach (ref readonly Bgra32 pixel in bytes)
                    {
                        b += pixel.B;
                        g += pixel.G;
                        r += pixel.R;
                        a += pixel.A;
                    }

                    return new((byte)(r / bytes.Length), (byte)(g / bytes.Length), (byte)(b / bytes.Length), (byte)(a / bytes.Length));
                }
            }
        }
    }
}
