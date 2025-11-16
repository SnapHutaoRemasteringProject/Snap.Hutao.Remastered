// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Win32.Foundation;
using System.IO;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Core.IO.HPatch;

internal unsafe struct StreamInput : IDisposable
{
#pragma warning disable CS0169
#pragma warning disable CA1823
    private GCHandle handle;
    private readonly ulong length;
    private readonly delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte*, BOOL> read;
    private readonly void* reserved;
#pragma warning restore CA1823
#pragma warning restore CS0169

    public StreamInput(FileSegment file)
    {
        handle = GCHandle.Alloc(file);
        length = (ulong)file.Length;
        read = &StreamIO.FileSegmentRead;
    }

    public StreamInput(Stream stream)
    {
        Verify.Operation(stream.CanSeek, "Input stream must support seeking.");
        handle = GCHandle.Alloc(stream);
        length = (ulong)stream.Length;
        read = &StreamIO.StreamRead;
    }

    public GCHandle<T> Handle<T>()
        where T : class
    {
        return GCHandle<T>.FromIntPtr(GCHandle.ToIntPtr(handle));
    }

    public BOOL Read(void* pThis, ulong position, byte* start, byte* end)
    {
        return read(pThis, position, start, end);
    }

    public void Dispose()
    {
        if (handle.IsAllocated)
        {
            handle.Free();
        }
    }
}