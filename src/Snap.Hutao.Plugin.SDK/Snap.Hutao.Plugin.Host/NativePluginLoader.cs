using Microsoft.UI.Xaml.Markup;
using WinRT;
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

    [LibraryImport("combase.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int WindowsCreateString(string source, uint length, out nint value);

    [LibraryImport("combase.dll")]
    private static partial int WindowsDeleteString(nint value);

    // Called only after the approved plugin module has already been mapped.
    internal static IXamlMetadataProvider GetMetadataProvider(string path, string className)
    {
        nint library;
        lock (Gate)
            if (!Modules.TryGetValue(Path.GetFullPath(path), out library))
                throw new InvalidOperationException("Plugin module is not mapped.");
        var getFactory = (delegate* unmanaged[Stdcall]<nint, nint*, int>)NativeLibrary.GetExport(library, "DllGetActivationFactory");
        nint name = 0, factory = 0, instance = 0, provider = 0;
        try
        {
            Marshal.ThrowExceptionForHR(WindowsCreateString(className, (uint)className.Length, out name));
            Marshal.ThrowExceptionForHR(getFactory(name, &factory));
            if (factory == 0) throw new COMException("Null XAML activation factory.", PluginAbi.PointerError);
            var activate = (delegate* unmanaged[Stdcall]<nint, nint*, int>)(*(nint**)factory)[6];
            Marshal.ThrowExceptionForHR(activate(factory, &instance));
            if (instance == 0) throw new COMException("Null XAML metadata instance.", PluginAbi.PointerError);
            Marshal.ThrowExceptionForHR(PluginAbi.Query(instance, typeof(IXamlMetadataProvider).GUID, out provider));
            if (provider == 0) throw new COMException("Null XAML metadata interface.", PluginAbi.PointerError);
            return MarshalInterface<IXamlMetadataProvider>.FromAbi(provider)
                ?? throw new COMException("Cannot project XAML metadata provider.", PluginAbi.PointerError);
        }
        finally
        {
            PluginAbi.Release(provider);
            PluginAbi.Release(instance);
            PluginAbi.Release(factory);
            if (name != 0) _ = WindowsDeleteString(name);
        }
    }

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
