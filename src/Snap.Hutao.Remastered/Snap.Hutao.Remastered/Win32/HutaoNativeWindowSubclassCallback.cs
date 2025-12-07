// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.UI.Windowing;
using Snap.Hutao.Remastered.Win32.Foundation;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Remastered.Win32;

internal readonly unsafe struct HutaoNativeWindowSubclassCallback
{
    private readonly delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, GCHandle<XamlWindowSubclass>, LRESULT*, BOOL> value;

    public HutaoNativeWindowSubclassCallback(delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, GCHandle<XamlWindowSubclass>, LRESULT*, BOOL> value)
    {
        this.value = value;
    }

    public static HutaoNativeWindowSubclassCallback Create(delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, GCHandle<XamlWindowSubclass>, LRESULT*, BOOL> method)
    {
        return new(method);
    }
}