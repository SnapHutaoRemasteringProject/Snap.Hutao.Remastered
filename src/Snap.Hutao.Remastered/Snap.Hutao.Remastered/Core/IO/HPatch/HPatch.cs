// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Win32.Foundation;
using System.IO;

namespace Snap.Hutao.Remastered.Core.IO.HPatch;

public static unsafe class HPatch
{
    private static readonly AppLocalNativeLibrary Library = new("Snap.HPatch.dll");

    public static long GetNewDataSize(FileSegment diff)
    {
        using StreamInput diffAdapter = new(diff);
        ulong size = 0;
        NewDataSize(&diffAdapter, &size);
        return (long)size;
    }

    public static bool Patch(FileSegment source, FileSegment diff, FileSegment target)
    {
        using StreamInput sourceAdapter = new(source);
        using StreamInput diffAdapter = new(diff);
        using StreamOutput targetAdapter = new(target);

        return PatchCore(&sourceAdapter, &diffAdapter, &targetAdapter);
    }

    public static bool PatchZstandard(FileSegment source, FileSegment diff, FileSegment target)
    {
        using StreamInput sourceAdapter = new(source);
        using StreamInput diffAdapter = new(diff);
        using StreamOutput targetAdapter = new(target);
        Decompress decompressor = Decompress.CreateZstandard();

        return PatchWithDecompressor(&sourceAdapter, &diffAdapter, &targetAdapter, &decompressor);
    }

    public static bool Patch(FileSegment source, FileSegment diff, Stream target)
    {
        using StreamInput sourceAdapter = new(source);
        using StreamInput diffAdapter = new(diff);
        using StreamOutput targetAdapter = new(target);

        return PatchCore(&sourceAdapter, &diffAdapter, &targetAdapter);
    }

    public static bool PatchZstandard(FileSegment source, FileSegment diff, Stream target)
    {
        using StreamInput sourceAdapter = new(source);
        using StreamInput diffAdapter = new(diff);
        using StreamOutput targetAdapter = new(target);
        Decompress decompressor = Decompress.CreateZstandard();

        return PatchWithDecompressor(&sourceAdapter, &diffAdapter, &targetAdapter, &decompressor);
    }

    private static readonly delegate* unmanaged[Stdcall]<StreamInput*, ulong*, BOOL> NewDataSize =
        (delegate* unmanaged[Stdcall]<StreamInput*, ulong*, BOOL>)Library.GetExport(nameof(NewDataSize));

    private static readonly delegate* unmanaged[Stdcall]<StreamInput*, StreamInput*, StreamOutput*, BOOL> PatchCore =
        (delegate* unmanaged[Stdcall]<StreamInput*, StreamInput*, StreamOutput*, BOOL>)Library.GetExport(nameof(Patch));

    private static readonly delegate* unmanaged[Stdcall]<StreamInput*, StreamInput*, StreamOutput*, Decompress*, BOOL> PatchWithDecompressor =
        (delegate* unmanaged[Stdcall]<StreamInput*, StreamInput*, StreamOutput*, Decompress*, BOOL>)Library.GetExport(nameof(PatchWithDecompressor));
}
