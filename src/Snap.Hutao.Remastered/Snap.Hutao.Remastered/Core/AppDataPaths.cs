// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Snap.Hutao.Remastered.Core;

internal static class AppDataPaths
{
    // A process-local profile lets the complete unpackaged application run against
    // a copied database/settings without opening the normal user's files.
    public static string? ProfileRoot { get; } = ResolveProfileRoot();

    public static string UnpackagedRoot { get; } = ProfileRoot
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SnapHutaoRemastered");

    public static string InstanceSuffix { get; } = ProfileRoot is null ? string.Empty
        : "." + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ProfileRoot.ToUpperInvariant()))).Substring(0, 24);

    private static string? ResolveProfileRoot()
    {
        string? root = Environment.GetEnvironmentVariable("SNAP_HUTAO_PROFILE_ROOT");
        if (string.IsNullOrWhiteSpace(root) || RuntimeEnvironment.IsPackaged)
        {
            return null;
        }

        if (!Path.IsPathFullyQualified(root))
        {
            throw new ArgumentException("SNAP_HUTAO_PROFILE_ROOT must be an absolute path.");
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
    }
}
