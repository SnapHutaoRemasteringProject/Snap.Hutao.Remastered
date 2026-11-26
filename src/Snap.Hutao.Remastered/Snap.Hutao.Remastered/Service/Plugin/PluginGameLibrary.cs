using Snap.Hutao.Plugin.Packaging;
using System.IO;
using System.Security.Cryptography;

namespace Snap.Hutao.Remastered.Service.Plugin;

// Immutable per-launch contribution, produced only from an approved, active package.
// This is an internal host record, not an ABI or arbitrary target-process service.
internal sealed record PluginGameLibrary(string PluginName, string RelativePath, string FullPath, string Sha256)
{
    internal FileStream OpenVerified()
    {
        PackagePaths.RejectReparseAncestors(FullPath);
        FileStream file = new(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            if (!Convert.ToHexStringLower(SHA256.HashData(file)).Equals(Sha256, StringComparison.Ordinal))
                throw new InvalidDataException("Approved game library changed: " + RelativePath);
            file.Position = 0;
            return file;
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }
}
