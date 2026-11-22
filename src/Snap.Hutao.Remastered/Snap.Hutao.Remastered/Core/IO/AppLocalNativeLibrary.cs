// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Win32LibraryFlags = global::Windows.Win32.System.LibraryLoader.LOAD_LIBRARY_FLAGS;
using Win32Module = global::Windows.Win32.Foundation.HMODULE;
using Win32PInvoke = global::Windows.Win32.PInvoke;

namespace Snap.Hutao.Remastered.Core.IO;

internal sealed class AppLocalNativeLibrary
{
    // The library stays loaded for the lifetime of its cached function pointers.
    private readonly Win32Module module;
    private readonly string libraryName;

    public AppLocalNativeLibrary(string libraryName)
    {
        this.libraryName = libraryName;
        string path = Path.Combine(AppContext.BaseDirectory, libraryName);
        module = Win32PInvoke.LoadLibraryEx(path, Win32LibraryFlags.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | Win32LibraryFlags.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
        if (module.IsNull)
        {
            throw new DllNotFoundException(path, new Win32Exception(Marshal.GetLastPInvokeError()));
        }
    }

    public nint GetExport(string name)
    {
        nint address = Win32PInvoke.GetProcAddress(module, name);
        return address != 0 ? address : throw new EntryPointNotFoundException($"{libraryName}!{name}");
    }
}
