// Copyright (c) Millennium-Science-Technology-R-D-Inst. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Snap.Hutao.Remastered.Core.IO;

internal static class ExecutableInfoHelper
{
    /// <summary>
    /// Get a friendly name for an executable file.
    /// Priority:
    /// 1. FileVersionInfo.ProductName
    /// 2. FileVersionInfo.FileDescription
    /// 3. AssemblyName.Name (for managed assemblies)
    /// 4. File name without extension
    /// </summary>
    /// <param name="path">Path to the executable file.</param>
    /// <returns>Friendly name (never null or empty).</returns>
    public static string GetFriendlyName(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Executable not found.", path);
        }

        try
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);

            if (!string.IsNullOrWhiteSpace(versionInfo.ProductName))
            {
                return versionInfo.ProductName;
            }

            if (!string.IsNullOrWhiteSpace(versionInfo.FileDescription))
            {
                return versionInfo.FileDescription;
            }
        }
        catch
        {
            // Ignore and try next fallback.
        }

        try
        {
            // For managed assemblies, try to read assembly name.
            AssemblyName asmName = AssemblyName.GetAssemblyName(path);
            if (!string.IsNullOrWhiteSpace(asmName.Name))
            {
                return asmName.Name;
            }
        }
        catch
        {
            // Not a managed assembly or cannot read; fallback to file name.
        }

        return Path.GetFileNameWithoutExtension(path) ?? path;
    }
}