// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.UI.Shell;
using Snap.Hutao.Win32.Foundation;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Win32;

internal readonly unsafe struct HutaoNativeNotifyIconCallback
{
    private readonly delegate* unmanaged[Stdcall]<HutaoNativeNotifyIconCallbackKind, RECT, POINT, GCHandle<NotifyIconController>, void> value;

    public HutaoNativeNotifyIconCallback(delegate* unmanaged[Stdcall]<HutaoNativeNotifyIconCallbackKind, RECT, POINT, GCHandle<NotifyIconController>, void> value)
    {
        this.value = value;
    }

    public static HutaoNativeNotifyIconCallback Create(delegate* unmanaged[Stdcall]<HutaoNativeNotifyIconCallbackKind, RECT, POINT, GCHandle<NotifyIconController>, void> method)
    {
        return new(method);
    }
}