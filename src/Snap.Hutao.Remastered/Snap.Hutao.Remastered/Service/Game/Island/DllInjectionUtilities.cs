// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Service.Game.Island;

public static unsafe class DllInjectionUtilities
{
    private static readonly AppLocalNativeLibrary Library = new(HutaoNativeMethods.DllName);

    public static unsafe void InjectUsingWindowsHook(ReadOnlySpan<char> dllPath, ReadOnlySpan<char> functionName, int processId)
    {
        fixed (char* pDllPath = dllPath)
        {
            fixed (char* pFunctionName = functionName)
            {
                Marshal.ThrowExceptionForHR(DllInjectionUtilitiesInjectUsingWindowsHook(pDllPath, pFunctionName, processId));
            }
        }
    }

    public static unsafe void InjectUsingWindowsHook2(ReadOnlySpan<char> dllPath, ReadOnlySpan<char> functionName, int processId)
    {
        fixed (char* pDllPath = dllPath)
        {
            fixed (char* pFunctionName = functionName)
            {
                Marshal.ThrowExceptionForHR(DllInjectionUtilitiesInjectUsingWindowsHook2(pDllPath, pFunctionName, processId));
            }
        }
    }

    public static unsafe void InjectUsingRemoteThread(ReadOnlySpan<char> dllPath, int processId)
    {
        fixed (char* pDllPath = dllPath)
        {
            Marshal.ThrowExceptionForHR(DllInjectionUtilitiesInjectUsingRemoteThread(pDllPath, processId));
        }
    }

    public static unsafe void InjectUsingRemoteThread(ReadOnlySpan<char> dllPath, ReadOnlySpan<char> functionName, int processId)
    {
        fixed (char* pDllPath = dllPath)
        {
            fixed (char* pFunctionName = functionName)
            {
                Marshal.ThrowExceptionForHR(DllInjectionUtilitiesInjectUsingRemoteThreadWithFunction(pDllPath, pFunctionName, processId));
            }
        }
    }

    private static readonly delegate* unmanaged[Stdcall]<PCWSTR, PCWSTR, int, HRESULT> DllInjectionUtilitiesInjectUsingWindowsHook =
        (delegate* unmanaged[Stdcall]<PCWSTR, PCWSTR, int, HRESULT>)Library.GetExport(nameof(DllInjectionUtilitiesInjectUsingWindowsHook));

    private static readonly delegate* unmanaged[Stdcall]<PCWSTR, PCWSTR, int, HRESULT> DllInjectionUtilitiesInjectUsingWindowsHook2 =
        (delegate* unmanaged[Stdcall]<PCWSTR, PCWSTR, int, HRESULT>)Library.GetExport(nameof(DllInjectionUtilitiesInjectUsingWindowsHook2));

    private static readonly delegate* unmanaged[Stdcall]<PCWSTR, int, HRESULT> DllInjectionUtilitiesInjectUsingRemoteThread =
        (delegate* unmanaged[Stdcall]<PCWSTR, int, HRESULT>)Library.GetExport(nameof(DllInjectionUtilitiesInjectUsingRemoteThread));

    private static readonly delegate* unmanaged[Stdcall]<PCWSTR, PCWSTR, int, HRESULT> DllInjectionUtilitiesInjectUsingRemoteThreadWithFunction =
        (delegate* unmanaged[Stdcall]<PCWSTR, PCWSTR, int, HRESULT>)Library.GetExport(nameof(DllInjectionUtilitiesInjectUsingRemoteThreadWithFunction));
}
