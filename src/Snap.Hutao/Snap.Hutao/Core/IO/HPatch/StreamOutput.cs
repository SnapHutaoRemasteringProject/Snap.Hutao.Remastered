// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Win32.Foundation;
using System.IO;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Core.IO.HPatch;

internal unsafe struct StreamOutput : IDisposable
{
#pragma warning disable CS0169
#pragma warning disable CS0649
    private GCHandle handle;
    private readonly ulong length;
    private readonly delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte*, BOOL> read;
    private readonly delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte*, BOOL> write;
#pragma warning restore CS0649
#pragma warning restore CS0169

    public StreamOutput(FileSegment file)
    {
        handle = GCHandle.Alloc(file);
        length = (ulong)file.Length;
        read = &StreamIO.FileSegmentRead;
        write = &StreamIO.FileSegmentWrite;
    }

    public StreamOutput(Stream stream)
    {
        Verify.Operation(stream.CanSeek, "Input stream must support seeking.");
        handle = GCHandle.Alloc(stream);
        length = (ulong)stream.Length;
        read = &StreamIO.StreamRead;
        write = &StreamIO.StreamWrite;
    }

    public GCHandle<T> Handle<T>()
        where T : class
    {
        return GCHandle<T>.FromIntPtr(GCHandle.ToIntPtr(handle));
    }

    public void Dispose()
    {
        if (handle.IsAllocated)
        {
            handle.Free();
        }
    }
}