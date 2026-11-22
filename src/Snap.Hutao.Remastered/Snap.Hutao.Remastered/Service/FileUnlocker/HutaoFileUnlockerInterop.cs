// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO;

namespace Snap.Hutao.Remastered.Service.FileUnlocker;

public static unsafe class HutaoFileUnlockerInterop
{
    private static readonly AppLocalNativeLibrary Library = new("Snap.Hutao.FileUnlocker.dll");

    private static readonly delegate* unmanaged[Stdcall]<char*, char**, int> QueryZoneIdentifier =
        (delegate* unmanaged[Stdcall]<char*, char**, int>)Library.GetExport(nameof(HutaoFileUnlocker_QueryZoneIdentifier));

    private static readonly delegate* unmanaged[Stdcall]<char*, byte, char**, int> RemoveZoneIdentifier =
        (delegate* unmanaged[Stdcall]<char*, byte, char**, int>)Library.GetExport(nameof(HutaoFileUnlocker_RemoveZoneIdentifier));

    private static readonly delegate* unmanaged[Stdcall]<char*, char**, int> QueryFileLocks =
        (delegate* unmanaged[Stdcall]<char*, char**, int>)Library.GetExport(nameof(HutaoFileUnlocker_QueryFileLocks));

    private static readonly delegate* unmanaged[Stdcall]<char*, byte, char**, int> UnlockFileLocks =
        (delegate* unmanaged[Stdcall]<char*, byte, char**, int>)Library.GetExport(nameof(HutaoFileUnlocker_UnlockFileLocks));

    private static readonly delegate* unmanaged[Stdcall]<char*, void> FreeString =
        (delegate* unmanaged[Stdcall]<char*, void>)Library.GetExport(nameof(HutaoFileUnlocker_FreeString));

    public static int HutaoFileUnlocker_QueryZoneIdentifier(char* path, char** metadataJson)
    {
        return QueryZoneIdentifier(path, metadataJson);
    }

    public static int HutaoFileUnlocker_RemoveZoneIdentifier(char* path, bool recursive, char** metadataJson)
    {
        return RemoveZoneIdentifier(path, recursive ? (byte)1 : (byte)0, metadataJson);
    }

    public static int HutaoFileUnlocker_QueryFileLocks(char* path, char** metadataJson)
    {
        return QueryFileLocks(path, metadataJson);
    }

    public static int HutaoFileUnlocker_UnlockFileLocks(char* path, bool force, char** metadataJson)
    {
        return UnlockFileLocks(path, force ? (byte)1 : (byte)0, metadataJson);
    }

    public static void HutaoFileUnlocker_FreeString(char* value)
    {
        FreeString(value);
    }
}
