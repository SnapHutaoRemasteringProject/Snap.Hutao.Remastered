// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Win32.Foundation;

namespace Snap.Hutao.Win32.System.WinRT;

internal static class IMemoryBufferByteAccessExtension
{
    extension(IMemoryBufferByteAccess memoryBufferByteAccess)
    {
        public unsafe HRESULT GetBuffer(out byte* value, out uint capacity)
        {
            fixed (byte** value2 = &value)
            {
                fixed (uint* capacity2 = &capacity)
                {
                    return memoryBufferByteAccess.GetBuffer(value2, capacity2);
                }
            }
        }

        public unsafe HRESULT GetBuffer<T>(out Span<T> value)
            where T : unmanaged
        {
            HRESULT retVal = memoryBufferByteAccess.GetBuffer(out byte* data, out uint capacity);
            value = new(data, unchecked((int)capacity / sizeof(T)));
            return retVal;
        }
    }
}