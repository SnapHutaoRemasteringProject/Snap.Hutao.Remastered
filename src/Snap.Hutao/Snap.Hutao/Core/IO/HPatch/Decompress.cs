// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.IO.Compression.Zstandard;
using Snap.Hutao.Win32.Foundation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Core.IO.HPatch;

internal unsafe struct Decompress
{
#pragma warning disable CS0169
    private readonly delegate* unmanaged[Cdecl]<PCSTR, BOOL> isCanOpen;
    private readonly delegate* unmanaged[Cdecl]<Decompress*, ulong, StreamInput*, ulong, ulong, GCHandle<ZstandardDecompressStream>> open;
    private readonly delegate* unmanaged[Cdecl]<Decompress*, GCHandle<ZstandardDecompressStream>, BOOL> close;
    private readonly delegate* unmanaged[Cdecl]<GCHandle<ZstandardDecompressStream>, byte*, byte*, BOOL> decompress;
    private readonly delegate* unmanaged[Cdecl]<nint, ulong, StreamInput*, ulong, ulong, BOOL> reset;
    private int error;
#pragma warning restore CS0169

    public Decompress(
        delegate* unmanaged[Cdecl]<PCSTR, BOOL> isCanOpen,
        delegate* unmanaged[Cdecl]<Decompress*, ulong, StreamInput*, ulong, ulong, GCHandle<ZstandardDecompressStream>> open,
        delegate* unmanaged[Cdecl]<Decompress*, GCHandle<ZstandardDecompressStream>, BOOL> close,
        delegate* unmanaged[Cdecl]<GCHandle<ZstandardDecompressStream>, byte*, byte*, BOOL> decompress,
        delegate* unmanaged[Cdecl]<nint, ulong, StreamInput*, ulong, ulong, BOOL> reset)
    {
        this.isCanOpen = isCanOpen;
        this.open = open;
        this.close = close;
        this.decompress = decompress;
        this.reset = reset;
    }

    public static Decompress CreateZstandard()
    {
        return new(&ZstandardIsCanOpen, &ZstandardOpen, &ZstandardClose, &ZstandardDecompress, null);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static BOOL ZstandardIsCanOpen(PCSTR compressType)
    {
        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(compressType).SequenceEqual("zstd"u8);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static GCHandle<ZstandardDecompressStream> ZstandardOpen(Decompress* decompressor, ulong dataSize, StreamInput* codeStream, ulong codeBegin, ulong codeEnd)
    {
        return new(new(new StreamInputStream(codeStream, codeBegin, codeEnd)));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static BOOL ZstandardClose(Decompress* decompressor, GCHandle<ZstandardDecompressStream> handle)
    {
        if (handle.Target is not { } stream)
        {
            return true;
        }

        stream.Dispose();
        handle.Dispose();
        return true;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static BOOL ZstandardDecompress(GCHandle<ZstandardDecompressStream> handle, byte* data, byte* dataEnd)
    {
        if (handle.Target is not { } stream)
        {
            return false;
        }

        try
        {
            stream.ReadExactly(new(data, (int)(dataEnd - data)));
            return true;
        }
        catch
        {
            return false;
        }
    }
}