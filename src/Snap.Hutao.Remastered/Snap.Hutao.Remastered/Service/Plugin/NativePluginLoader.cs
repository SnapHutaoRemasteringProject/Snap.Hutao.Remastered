using System.IO;
using Microsoft.UI.Xaml.Markup;
using WinRT;
using Snap.Hutao.Plugin.Contracts;
using Snap.Hutao.Plugin.Projection;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Win32LibraryFlags = global::Windows.Win32.System.LibraryLoader.LOAD_LIBRARY_FLAGS;
using Win32Module = global::Windows.Win32.Foundation.HMODULE;
using Win32HString = global::Windows.Win32.System.WinRT.HSTRING;
using Win32PInvoke = global::Windows.Win32.PInvoke;

namespace Snap.Hutao.Remastered.Service.Plugin;

internal static unsafe class NativePluginLoader
{
    private static readonly Dictionary<string, Win32Module> Modules = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Gate = new();
    private const Win32LibraryFlags SearchFlags = Win32LibraryFlags.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR |
        Win32LibraryFlags.LOAD_LIBRARY_SEARCH_APPLICATION_DIR | Win32LibraryFlags.LOAD_LIBRARY_SEARCH_SYSTEM32;

    private static nint GetExport(Win32Module library, string path, string name)
    {
        nint address = Win32PInvoke.GetProcAddress(library, name);
        if (address == 0)
            throw new EntryPointNotFoundException($"{path}!{name}", new Win32Exception(Marshal.GetLastPInvokeError()));
        return address;
    }

    // Called only after the approved plugin module has already been mapped.
    internal static IXamlMetadataProvider GetMetadataProvider(string path, string className)
    {
        Win32Module library;
        lock (Gate)
            if (!Modules.TryGetValue(Path.GetFullPath(path), out library))
                throw new InvalidOperationException("Plugin module is not mapped.");
        var getFactory = (delegate* unmanaged[Stdcall]<nint, nint*, int>)GetExport(library, path, "DllGetActivationFactory");
        Win32HString name = default;
        nint factory = 0, instance = 0, provider = 0;
        try
        {
            fixed (char* source = className)
            {
                Marshal.ThrowExceptionForHR((int)Win32PInvoke.WindowsCreateString(source, (uint)className.Length, &name));
            }
            Marshal.ThrowExceptionForHR(getFactory((nint)name.Value, &factory));
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
            _ = Win32PInvoke.WindowsDeleteString(name);
        }
    }

    public static IHutaoPlugin Load(string path)
    {
        path = Path.GetFullPath(path);
        Win32Module library;
        lock (Gate)
        {
            if (!Modules.TryGetValue(path, out library))
            {
                library = Win32PInvoke.LoadLibraryEx(path, SearchFlags);
                if (library.IsNull) throw new Win32Exception(Marshal.GetLastPInvokeError());
                Modules.Add(path, library); // Never unload mapped modules.
            }
        }

        var create = (delegate* unmanaged[Cdecl]<uint, nint*, int>)GetExport(library, path, "HutaoPluginCreate");
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
