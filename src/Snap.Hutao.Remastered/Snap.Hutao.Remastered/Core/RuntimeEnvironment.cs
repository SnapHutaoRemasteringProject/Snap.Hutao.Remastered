// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32;
using System.Diagnostics;
using Windows.ApplicationModel;

namespace Snap.Hutao.Remastered.Core;

public static class RuntimeEnvironment
{
    public static bool IsPackaged { get; }
    public static bool IsUnpackaged => !IsPackaged;

    static RuntimeEnvironment()
    {
        try
        {
            _ = Package.Current;
            IsPackaged = true;
        }
        catch
        {
            IsPackaged = false;
        }
    }

    public static void TryRegisterProtocol()
    {
        if (IsPackaged || AppDataPaths.ProfileRoot is not null)
        {
            // MSIX owns packaged registration. An explicit test/profile directory
            // must not replace the normal installation's system-wide URI handler.
            return;
        }

        try
        {
            string? executablePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(executablePath))
            {
                return;
            }

            using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\hutao"))
            {
                if (key is null)
                {
                    return;
                }

                key.SetValue(string.Empty, "URL:hutao");
                key.SetValue("URL Protocol", string.Empty);

                using (RegistryKey? commandKey = key.CreateSubKey(@"shell\open\command"))
                {
                    commandKey?.SetValue(string.Empty, $"\"{executablePath}\" \"%1\"");
                }
            }
        }
        catch
        {
            // Best effort registration
        }
    }
}
