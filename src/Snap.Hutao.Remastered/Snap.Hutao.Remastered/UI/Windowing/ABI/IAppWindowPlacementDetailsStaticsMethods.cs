// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;
using WinRT;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130
namespace ABI.Microsoft.UI.Windowing;
#pragma warning restore IDE0130

public static class IAppWindowPlacementDetailsStaticsMethods
{
    private static readonly Guid InterfaceId = new("f19f1745-52ad-5cf7-97ea-76c5fd6ff3c1");

    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref InterfaceId;
    }

    public static unsafe global::Microsoft.UI.Windowing.AppWindowPlacementDetails Create(IObjectReference obj, global::Windows.Graphics.RectInt32 normalRect, global::Windows.Graphics.RectInt32 workArea, int dpi, int showCmd, global::Windows.Graphics.RectInt32 arrangeRect, global::Microsoft.UI.Windowing.PlacementInfo flags, string deviceName)
    {
        nint thisPtr = obj.ThisPtr;

        nint retval = default;
        try
        {
            MarshalString.Pinnable deviceNameValue = new(deviceName);
            fixed (void* pDeviceName = deviceNameValue)
            {
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<nint, global::Windows.Graphics.RectInt32, global::Windows.Graphics.RectInt32, int, int, global::Windows.Graphics.RectInt32, global::Microsoft.UI.Windowing.PlacementInfo, nint, nint*, int>**)thisPtr)[6](thisPtr, normalRect, workArea, dpi, showCmd, arrangeRect, flags, MarshalString.GetAbi(ref deviceNameValue), &retval));
                GC.KeepAlive(obj);
                return AppWindowPlacementDetails.FromAbi(retval);
            }
        }
        finally
        {
            AppWindowPlacementDetails.DisposeAbi(retval);
        }
    }
}
