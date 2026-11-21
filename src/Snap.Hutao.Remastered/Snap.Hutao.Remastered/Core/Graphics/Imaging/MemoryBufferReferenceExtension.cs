// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Windows.Foundation;
using WinRT;
using WinRT.Interop;

namespace Snap.Hutao.Remastered.Core.Graphics.Imaging;

internal static class MemoryBufferReferenceExtension
{
    private static readonly Guid ByteAccessIid = new("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D");

    // The caller keeps the reference alive while consuming this span. QueryInterface
    // and slot 3 replace the classic COM RCW, which Native AOT cannot construct.
    public static Span<T> GetBuffer<T>(this IMemoryBufferReference reference)
        where T : unmanaged
    {
        using IObjectReference access = ((IWinRTObject)reference).NativeObject.As<IUnknownVftbl>(ByteAccessIid);
        return MemoryBufferByteAccess.GetBuffer<T>(access.ThisPtr);
    }
}
