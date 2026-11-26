using System.IO;
using System.Security.Cryptography;
using Snap.Hutao.Plugin.Packaging;
using Snap.Hutao.Remastered.Core;

namespace Snap.Hutao.Remastered.Service.Plugin;

internal sealed record InstalledPlugin(string Id, string Path, bool Enabled, PluginManifest? Manifest, string? Hash, string? Error, bool Conflict = false);
internal sealed record PluginDiskState(string? ApprovedHash = null, string? HookHash = null, string? PendingHash = null, string? BackupHash = null, string? LastError = null);

internal static class PluginPackageStore
{
    internal static string Root => Path.Combine(HutaoRuntime.DataDirectory, "Plugins");
    internal static string StateRoot => Path.Combine(Root, ".sdk2-state");
    internal static string StateDirectory(string id)
    {
        PackagePaths.ValidateIdentifier(id);
        string path = Path.Combine(StateRoot, id);
        PackagePaths.RejectReparseAncestors(path);
        return path;
    }

    internal static string PathFor(string id, bool enabled)
    {
        PackagePaths.ValidateIdentifier(id);
        string path = Path.Combine(Root, id + (enabled ? ".hutao" : ".hutaodisabled"));
        PackagePaths.RejectReparseAncestors(path);
        return path;
    }

    internal static InstalledPlugin[] Scan()
    {
        PackagePaths.RejectReparseAncestors(Root);
        Directory.CreateDirectory(Root);
        return Directory.EnumerateFiles(Root)
            .Where(path => Path.GetExtension(path).Equals(".hutao", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".hutaodisabled", StringComparison.OrdinalIgnoreCase))
            .GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group =>
            {
                bool conflict = group.Count() != 1;
                return group.Select(path => Inspect(path, group.Key, conflict));
            }).ToArray();
    }

    internal static InstalledPlugin Inspect(string path, string id, bool conflict = false)
    {
        bool enabled = Path.GetExtension(path).Equals(".hutao", StringComparison.OrdinalIgnoreCase);
        try
        {
            PackagePaths.ValidateIdentifier(id);
            PackagePaths.RejectReparseAncestors(path);
            VerifiedPackage package = PluginPackage.Verify(path);
            if (!package.Manifest.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Manifest ID differs from the installed filename.");
            return new(id, path, enabled, package.Manifest, package.Sha256, conflict ? "Conflicting package files." : null, conflict);
        }
        catch (Exception error)
        {
            return new(id, path, enabled, null, null, error.Message, conflict);
        }
    }

    internal static InstalledPlugin Locate(string id)
    {
        InstalledPlugin[] matches = Scan().Where(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1 || matches[0].Conflict) throw new InvalidOperationException("Missing or conflicting package: " + id);
        return matches[0];
    }

    internal static PluginDiskState ReadState(string id)
    {
        string path = Path.Combine(StateDirectory(id), "state.json");
        PackagePaths.RejectReparseAncestors(path);
        if (!File.Exists(path)) return new();
        using FileStream input = File.OpenRead(path);
        if (input.Length > 65536) throw new InvalidDataException("Plugin state is too large.");
        using JsonDocument doc = JsonDocument.Parse(input);
        string? Read(string key) => doc.RootElement.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        return new(Read("approvedPackageSha256"), Read("installHookCompletedSha256"), Read("pendingPackageSha256"), Read("backupPackageSha256"), Read("lastError"));
    }

    internal static void WriteState(string id, PluginDiskState state)
    {
        string directory = StateDirectory(id);
        Directory.CreateDirectory(directory);
        string target = Path.Combine(directory, "state.json");
        PackagePaths.RejectReparseAncestors(target);
        string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (FileStream file = new(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using Utf8JsonWriter writer = new(file);
                writer.WriteStartObject();
                writer.WriteString("approvedPackageSha256", state.ApprovedHash);
                writer.WriteString("installHookCompletedSha256", state.HookHash);
                writer.WriteString("pendingPackageSha256", state.PendingHash);
                writer.WriteString("backupPackageSha256", state.BackupHash);
                writer.WriteString("lastError", state.LastError);
                writer.WriteEndObject();
                writer.Flush();
                file.Flush(flushToDisk: true);
            }
            File.Move(temp, target, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    internal static void CopyApproved(string source, string target, string expectedHash, bool overwrite = false)
    {
        PackagePaths.RejectReparseAncestors(source);
        PackagePaths.RejectReparseAncestors(target);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (FileStream input = File.OpenRead(source))
            using (FileStream output = new(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                if (input.Length > 256L * 1024 * 1024) throw new InvalidDataException("Plugin archive exceeds 256 MiB.");
                input.CopyTo(output);
                output.Flush(flushToDisk: true);
            }
            if (!PluginPackage.Verify(temp).Sha256.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Package differs from the approved SHA256.");
            File.Move(temp, target, overwrite);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    internal static void VerifyWorkspace(PluginIdentity identity, PluginManifest manifest)
    {
        foreach (var file in manifest.Files)
        {
            string path = PackagePaths.Resolve(identity.PackageDirectory, file.Key);
            PackagePaths.RejectReparseAncestors(path);
            using FileStream input = File.OpenRead(path);
            if (!Convert.ToHexStringLower(SHA256.HashData(input)).Equals(file.Value, StringComparison.Ordinal))
                throw new InvalidDataException("Mapped workspace changed; restart required: " + file.Key);
        }
    }
}
