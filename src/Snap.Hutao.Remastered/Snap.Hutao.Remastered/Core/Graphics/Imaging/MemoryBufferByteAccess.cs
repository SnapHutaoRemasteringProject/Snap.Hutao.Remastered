// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Core.Graphics.Imaging;

internal static class MemoryBufferByteAccess
{
    public static unsafe Span<T> GetBuffer<T>(nint instance)
        where T : unmanaged
    {
        byte* data = null;
        uint capacity = 0;
        nint* vtable = *(nint**)instance;
        int result = ((delegate* unmanaged[Stdcall]<nint, byte**, uint*, int>)vtable[3])(instance, &data, &capacity);
        Marshal.ThrowExceptionForHR(result);
        if (data is null && capacity != 0)
        {
            throw new InvalidOperationException("The bitmap buffer returned a null address with nonzero capacity.");
        }

        return new Span<T>(data, checked((int)(capacity / (uint)sizeof(T))));
    }
}
