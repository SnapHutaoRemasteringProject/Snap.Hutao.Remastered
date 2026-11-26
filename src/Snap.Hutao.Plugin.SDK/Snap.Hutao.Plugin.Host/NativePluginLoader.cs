using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Projection;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Snap.Hutao.Plugin.Host;

internal static unsafe partial class NativePluginLoader
{
    private static readonly Dictionary<string, nint> Modules = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Gate = new();
    private const uint SearchFlags = 0x00000100 | 0x00000200 | 0x00000800;

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint LoadLibraryExW(string path, nint reserved, uint flags);

    public static IHutaoPlugin Load(string path)
    {
        path = Path.GetFullPath(path);
        nint library;
        lock (Gate)
        {
            if (!Modules.TryGetValue(path, out library))
            {
                library = LoadLibraryExW(path, 0, SearchFlags);
                if (library == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
                Modules.Add(path, library); // Never unload mapped modules.
            }
        }

        var create = (delegate* unmanaged[Cdecl]<uint, nint*, int>)NativeLibrary.GetExport(library, "HutaoPluginCreate");
        nint plugin = 0;
        int hr = create(PluginAbi.Major, &plugin);
        try
        {
            if (hr != 0 || plugin == 0)
                throw new COMException("Invalid HutaoPluginCreate result.", hr < 0 ? hr : PluginAbi.PointerError);
            return PluginAbi.Import(plugin);
        }
        finally { PluginAbi.Release(plugin); }
    }
}
