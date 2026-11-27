using System.Runtime.InteropServices;
using Snap.Hutao.Plugin.Contracts;
using WinRT;

namespace Snap.Hutao.Plugin.Projection;

public static unsafe class PluginAbi
{
    public const uint Major = 1;
    public const int RevisionMismatch = unchecked((int)0x8007051A);
    public const int PointerError = unchecked((int)0x80004003);
    public static readonly Guid InspectableId = new("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90");
    public static readonly Guid PluginId = new("790c6149-9ca4-4ec0-a1f7-71cfbe03d45e");
    public static readonly Guid AgileId = new("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90");

    public static int Query(nint value, Guid iid, out nint result)
    {
        nint queried = 0;
        var table = *(nint**)value;
        int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)table[0])(value, &iid, &queried);
        result = queried;
        return hr;
    }

    public static void Release(nint value)
    {
        if (value != 0)
        {
            var table = *(nint**)value;
            _ = ((delegate* unmanaged[Stdcall]<nint, uint>)table[2])(value);
        }
    }

    public static nint Export(IHutaoPlugin plugin)
    {
        nint reference = MarshalInterface<IHutaoPlugin>.FromManaged(plugin);
        if (reference == 0) throw new COMException("Missing CCW.", PointerError);
        try
        {
            int hr = Query(reference, InspectableId, out nint inspectable);
            if (hr != 0 || inspectable == 0)
            {
                Release(inspectable);
                throw new COMException("Cannot obtain IInspectable.", hr < 0 ? hr : PointerError);
            }
            return inspectable;
        }
        finally { Release(reference); }
    }

    public static IHutaoPlugin Import(nint inspectable)
    {
        int hr = Query(inspectable, PluginId, out nint reference);
        try
        {
            if (hr != 0 || reference == 0)
                throw new COMException("Missing IHutaoPlugin.", hr < 0 ? hr : PointerError);
            return MarshalInterface<IHutaoPlugin>.FromAbi(reference)
                ?? throw new COMException("Projection returned null.", PointerError);
        }
        finally { Release(reference); }
    }

    public static void RequireAgile<T>(T value) where T : class
    {
        nint pointer = MarshalInterface<T>.FromManaged(value);
        if (pointer == 0) throw new COMException("Missing interface pointer.", PointerError);
        try
        {
            int hr = Query(pointer, AgileId, out nint agile);
            try
            {
                if (hr != 0 || agile == 0)
                    throw new COMException("Cross-thread interface is not agile.", hr < 0 ? hr : PointerError);
            }
            finally { Release(agile); }
        }
        finally { Release(pointer); }
    }
}
