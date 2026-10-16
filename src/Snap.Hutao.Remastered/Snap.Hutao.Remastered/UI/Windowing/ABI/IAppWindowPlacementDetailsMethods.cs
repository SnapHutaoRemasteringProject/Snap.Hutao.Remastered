// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;
using WinRT;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130
namespace ABI.Microsoft.UI.Windowing;
#pragma warning restore IDE0130

[SuppressMessage("", "SA1300")]
[SuppressMessage("", "IDE1006")]
public static class IAppWindowPlacementDetailsMethods
{
    private static readonly Guid InterfaceId = new("639ec5b2-ac0c-5bbf-8422-98dca540d219");

    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref InterfaceId;
    }

    public static unsafe global::Windows.Graphics.RectInt32 get_ArrangeRect(IObjectReference obj)
    {
        nint thisPtr = obj.ThisPtr;

        global::Windows.Graphics.RectInt32 retval = default;
        ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<nint, global::Windows.Graphics.RectInt32*, int>**)thisPtr)[10](thisPtr, &retval));
        GC.KeepAlive(obj);
        return retval;
    }

    public static unsafe string get_DeviceName(IObjectReference obj)
    {
        nint thisPtr = obj.ThisPtr;

        nint retval = default;
        try
        {
            ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<nint, nint*, int>**)thisPtr)[11](thisPtr, &retval));
            GC.KeepAlive(obj);
            return MarshalString.FromAbi(retval);
        }
        finally
        {
            MarshalString.DisposeAbi(retval);
        }
    }

    public static unsafe int get_Dpi(IObjectReference obj)
    {
        nint thisPtr = obj.ThisPtr;

        int retval = default;
        ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<nint, int*, int>**)thisPtr)[8](thisPtr, &retval));
        GC.KeepAlive(obj);
        return retval;
    }

    public static unsafe global::Microsoft.UI.Windowing.PlacementInfo get_Flags(IObjectReference obj)
    {
        nint thisPtr = obj.ThisPtr;

        global::Microsoft.UI.Windowing.PlacementInfo retval = default;
        ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<nint, global::Microsoft.UI.Windowing.PlacementInfo*, int>**)thisPtr)[12](thisPtr, &retval));
        GC.KeepAlive(obj);
        return retval;
    }

    public static unsafe global::Windows.Graphics.RectInt32 get_NormalRect(IObjectReference obj)
    {
        nint thisPtr = obj.ThisPtr;

        global::Windows.Graphics.RectInt32 retval = default;
        ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<nint, global::Windows.Graphics.RectInt32*, int>**)thisPtr)[6](thisPtr, &retval));
        GC.KeepAlive(obj);
        return retval;
    }

    public static unsafe int get_ShowCmd(IObjectReference obj)
    {
        nint thisPtr = obj.ThisPtr;

        int retval = default;
        ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<nint, int*, int>**)thisPtr)[9](thisPtr, &retval));
        GC.KeepAlive(obj);
        return retval;
    }

    public static unsafe global::Windows.Graphics.RectInt32 get_WorkArea(IObjectReference obj)
    {
        nint thisPtr = obj.ThisPtr;

        global::Windows.Graphics.RectInt32 retval = default;
        ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<nint, global::Windows.Graphics.RectInt32*, int>**)thisPtr)[7](thisPtr, &retval));
        GC.KeepAlive(obj);
        return retval;
    }
}
