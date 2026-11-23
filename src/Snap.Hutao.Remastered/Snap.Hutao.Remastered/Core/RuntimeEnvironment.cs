// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32;
using System.Diagnostics;
using Windows.ApplicationModel;

namespace Snap.Hutao.Remastered.Core;

public static class RuntimeEnvironment
{
    private const string ProtocolKeyPath = @"Software\Classes\hutao";

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
            using Process process = Process.GetCurrentProcess();
            string? executablePath = process.MainModule?.FileName;
            if (string.IsNullOrEmpty(executablePath))
            {
                return;
            }

            SetProtocolValueIfChanged(ProtocolKeyPath, string.Empty, "URL:hutao");
            SetProtocolValueIfChanged(ProtocolKeyPath, "URL Protocol", string.Empty);
            SetProtocolValueIfChanged($@"{ProtocolKeyPath}\shell\open\command", string.Empty, $"\"{executablePath}\" \"%1\"");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Could not register the hutao URL protocol: {ex.Message}");
        }
    }

    private static void SetProtocolValueIfChanged(string path, string name, string value)
    {
        using RegistryKey? existing = Registry.CurrentUser.OpenSubKey(path, writable: false);
        if (existing?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) is string current
            && string.Equals(current, value, StringComparison.Ordinal)
            && existing.GetValueKind(name) == RegistryValueKind.String)
        {
            return;
        }

        using RegistryKey? writable = Registry.CurrentUser.CreateSubKey(path);
        writable?.SetValue(name, value, RegistryValueKind.String);
    }
}
