// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Service.FileUnlocker;

[SuppressMessage("", "SYSLIB1054")]
public static unsafe class HutaoFileUnlockerInterop
{
    private const string DllName = "Snap.Hutao.FileUnlocker.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern int HutaoFileUnlocker_QueryZoneIdentifier(char* path, char** metadataJson);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern int HutaoFileUnlocker_RemoveZoneIdentifier(char* path, [MarshalAs(UnmanagedType.U1)] bool recursive, char** metadataJson);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern int HutaoFileUnlocker_QueryFileLocks(char* path, char** metadataJson);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern int HutaoFileUnlocker_UnlockFileLocks(char* path, [MarshalAs(UnmanagedType.U1)] bool force, char** metadataJson);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern void HutaoFileUnlocker_FreeString(char* value);
}
