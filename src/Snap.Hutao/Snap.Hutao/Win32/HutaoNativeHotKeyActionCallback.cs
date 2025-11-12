// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.UI.Input.HotKey;
using Snap.Hutao.Win32.Foundation;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Win32;

internal readonly unsafe struct HutaoNativeHotKeyActionCallback
{
    private readonly delegate* unmanaged[Stdcall]<BOOL, GCHandle<HotKeyCombination>, void> value;

    public HutaoNativeHotKeyActionCallback(delegate* unmanaged[Stdcall]<BOOL, GCHandle<HotKeyCombination>, void> value)
    {
        this.value = value;
    }

    public static HutaoNativeHotKeyActionCallback Create(delegate* unmanaged[Stdcall]<BOOL, GCHandle<HotKeyCombination>, void> method)
    {
        return new(method);
    }
}