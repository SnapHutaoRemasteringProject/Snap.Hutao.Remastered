// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.IO;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.UI.Windowing;

public static unsafe class WindowUtilities
{
    private static readonly AppLocalNativeLibrary Library = new(HutaoNativeMethods.DllName);

    public static void SwitchToWindow(HWND hWnd)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesSwitchToWindow(hWnd));
    }

    public static void AddExtendedStyleLayered(HWND hWnd)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesAddExtendedStyleLayered(hWnd));
    }

    public static void RemoveExtendedStyleLayered(HWND hWnd)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesRemoveExtendedStyleLayered(hWnd));
    }

    public static void SetLayeredWindowTransparency(HWND hWnd, byte opacity)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesSetLayeredWindowTransparency(hWnd, opacity));
    }

    public static void AddExtendedStyleToolWindow(HWND hWnd)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesAddExtendedStyleToolWindow(hWnd));
    }

    public static void RemoveStyleOverlappedWindow(HWND hWnd)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesRemoveStyleOverlappedWindow(hWnd));
    }

    public static float GetRasterizationScaleForWindow(HWND hWnd)
    {
        float scale;
        Marshal.ThrowExceptionForHR(WindowUtilitiesGetRasterizationScaleForWindow(hWnd, &scale));
        return scale;
    }

    public static void SetWindowIsEnabled(HWND hWnd, BOOL isEnabled)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesSetWindowIsEnabled(hWnd, isEnabled));
    }

    public static void SetWindowOwner(HWND hWnd, HWND hWndOwner)
    {
        Marshal.ThrowExceptionForHR(WindowUtilitiesSetWindowOwner(hWnd, hWndOwner));
    }

    private static readonly delegate* unmanaged[Stdcall]<HWND, HRESULT> WindowUtilitiesSwitchToWindow =
        (delegate* unmanaged[Stdcall]<HWND, HRESULT>)Library.GetExport(nameof(WindowUtilitiesSwitchToWindow));

    private static readonly delegate* unmanaged[Stdcall]<HWND, HRESULT> WindowUtilitiesAddExtendedStyleLayered =
        (delegate* unmanaged[Stdcall]<HWND, HRESULT>)Library.GetExport(nameof(WindowUtilitiesAddExtendedStyleLayered));

    private static readonly delegate* unmanaged[Stdcall]<HWND, HRESULT> WindowUtilitiesRemoveExtendedStyleLayered =
        (delegate* unmanaged[Stdcall]<HWND, HRESULT>)Library.GetExport(nameof(WindowUtilitiesRemoveExtendedStyleLayered));

    private static readonly delegate* unmanaged[Stdcall]<HWND, byte, HRESULT> WindowUtilitiesSetLayeredWindowTransparency =
        (delegate* unmanaged[Stdcall]<HWND, byte, HRESULT>)Library.GetExport(nameof(WindowUtilitiesSetLayeredWindowTransparency));

    private static readonly delegate* unmanaged[Stdcall]<HWND, HRESULT> WindowUtilitiesAddExtendedStyleToolWindow =
        (delegate* unmanaged[Stdcall]<HWND, HRESULT>)Library.GetExport(nameof(WindowUtilitiesAddExtendedStyleToolWindow));

    private static readonly delegate* unmanaged[Stdcall]<HWND, HRESULT> WindowUtilitiesRemoveStyleOverlappedWindow =
        (delegate* unmanaged[Stdcall]<HWND, HRESULT>)Library.GetExport(nameof(WindowUtilitiesRemoveStyleOverlappedWindow));

    private static readonly delegate* unmanaged[Stdcall]<HWND, float*, HRESULT> WindowUtilitiesGetRasterizationScaleForWindow =
        (delegate* unmanaged[Stdcall]<HWND, float*, HRESULT>)Library.GetExport(nameof(WindowUtilitiesGetRasterizationScaleForWindow));

    private static readonly delegate* unmanaged[Stdcall]<HWND, BOOL, HRESULT> WindowUtilitiesSetWindowIsEnabled =
        (delegate* unmanaged[Stdcall]<HWND, BOOL, HRESULT>)Library.GetExport(nameof(WindowUtilitiesSetWindowIsEnabled));

    private static readonly delegate* unmanaged[Stdcall]<HWND, HWND, HRESULT> WindowUtilitiesSetWindowOwner =
        (delegate* unmanaged[Stdcall]<HWND, HWND, HRESULT>)Library.GetExport(nameof(WindowUtilitiesSetWindowOwner));
}
