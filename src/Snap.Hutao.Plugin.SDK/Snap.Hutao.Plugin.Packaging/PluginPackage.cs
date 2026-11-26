using System.IO.Compression;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

namespace Snap.Hutao.Plugin.Packaging;

public sealed record VerifiedPackage(PluginManifest Manifest, string Sha256);

public static class PluginPackage
{
    private const long MiB = 1024 * 1024;
    private const long MaxArchive = 256 * MiB, MaxEntry = 256 * MiB, MaxExpanded = 512 * MiB;
    private static readonly DateTimeOffset Epoch = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static VerifiedPackage Verify(string path)
    {
        using FileStream input = OpenPackage(path);
        string hash = Hash(input);
        input.Position = 0;
        return new(ReadArchive(input, null), hash);
    }

    // Approval is an explicit caller responsibility. Validate the exact open archive,
    // then extract only verified contents into a fresh, process-owned directory.
    public static VerifiedPackage ExtractApproved(string path, string expectedSha256, string workspace, Version hostVersion)
    {
        using FileStream input = OpenPackage(path);
        string hash = Hash(input);
        if (!hash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Package differs from approved SHA256.");
        input.Position = 0;
        PluginManifest manifest = ReadArchive(input, null);
        if (manifest.MinHostVersion > hostVersion) throw new InvalidDataException("Host version too old.");
        workspace = Path.GetFullPath(workspace);
        PackagePaths.RejectReparseAncestors(workspace);
        if (Directory.Exists(workspace) || File.Exists(workspace)) throw new IOException("Workspace must be new.");
        Directory.CreateDirectory(workspace);
        try
        {
            input.Position = 0;
            _ = ReadArchive(input, workspace);
            return new(manifest, hash);
        }
        catch
        {
            Directory.Delete(workspace, recursive: true);
            throw;
        }
    }

    public static void Pack(string templatePath, string stagingDirectory, string outputPath)
    {
        byte[] manifestBytes = File.ReadAllBytes(templatePath);
        PluginManifest manifest = PluginManifest.Parse(manifestBytes, template: true);
        string staging = Path.GetFullPath(stagingDirectory);
        PackagePaths.RejectReparseAncestors(staging);
        string output = Path.GetFullPath(outputPath);
        if (output.StartsWith(Path.TrimEndingDirectorySeparator(staging) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Output must be outside staging.");
        if (!Path.GetExtension(output).Equals(".hutao", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Output must be .hutao.");
        Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
        foreach (string file in EnumerateFiles(staging))
        {
            string relative = Path.GetRelativePath(staging, file).Replace(Path.DirectorySeparatorChar, '/');
            PackagePaths.ValidateRelativePath(relative);
            if (relative.Equals("manifest.json", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Pass the manifest template separately.");
            if (!files.TryAdd(relative, file)) throw new InvalidDataException("Duplicate path.");
        }
        if (!files.TryGetValue(manifest.Entry, out string? dll)) throw new InvalidDataException("Missing entry DLL.");
        using (FileStream pe = File.OpenRead(dll)) ValidateNativeDll(pe);
        if (files.Count > 4095) throw new InvalidDataException("Too many files.");
        string temp = output + "." + Guid.NewGuid().ToString("N") + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        try
        {
            using (FileStream stream = new(temp, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                using ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true);
                List<(string Path, string Hash)> hashes = [];
                long total = 0;
                foreach (var item in files.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    using FileStream source = File.OpenRead(item.Value);
                    if (source.Length > Limit(item.Key, manifest.IconPath)) throw new InvalidDataException("Entry too large.");
                    total = checked(total + source.Length);
                    if (total > MaxExpanded) throw new InvalidDataException("Package exceeds expanded size limit.");
                    ZipArchiveEntry entry = CreateEntry(archive, item.Key);
                    using Stream target = entry.Open();
                    string hash = CopyAndHash(source, target, Limit(item.Key, manifest.IconPath), out _);
                    hashes.Add((item.Key, hash));
                }
                ReadOnlyMemory<byte> jsonBytes = manifestBytes;
                if (manifestBytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) jsonBytes = jsonBytes[3..];
                using JsonDocument json = JsonDocument.Parse(jsonBytes);
                using Stream meta = CreateEntry(archive, "manifest.json").Open();
                using Utf8JsonWriter writer = new(meta);
                writer.WriteStartObject();
                foreach (JsonProperty property in json.RootElement.EnumerateObject())
                    if (!property.Name.Equals("files", StringComparison.OrdinalIgnoreCase)) property.WriteTo(writer);
                writer.WriteStartArray("files");
                foreach (var item in hashes)
                {
                    writer.WriteStartObject();
                    writer.WriteString("path", item.Path);
                    writer.WriteString("sha256", item.Hash);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.Flush();
            }
            _ = Verify(temp);
            File.Move(temp, output, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static IEnumerable<string> EnumerateFiles(string directory)
    {
        foreach (string path in Directory.EnumerateFileSystemEntries(directory))
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Reparse point in staging.");
            if ((attributes & FileAttributes.Directory) != 0)
            {
                foreach (string file in EnumerateFiles(path)) yield return file;
            }
            else yield return path;
        }
    }

    private static FileStream OpenPackage(string path)
    {
        FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaxArchive) { stream.Dispose(); throw new InvalidDataException("Archive too large."); }
        return stream;
    }

    private static PluginManifest ReadArchive(Stream input, string? workspace)
    {
        using ZipArchive archive = new(input, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count is < 1 or > 4096) throw new InvalidDataException("Invalid entry count.");
        Dictionary<string, ZipArchiveEntry> entries = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> allPaths = new(StringComparer.OrdinalIgnoreCase);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            bool directory = entry.FullName.EndsWith('/');
            string path = directory ? entry.FullName[..^1] : entry.FullName;
            PackagePaths.ValidateRelativePath(path);
            int unixType = (entry.ExternalAttributes >> 16) & 0xF000;
            if (unixType != 0 && unixType != (directory ? 0x4000 : 0x8000) ||
                (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0 || !allPaths.Add(path))
                throw new InvalidDataException("Invalid or duplicate ZIP entry: " + path);
            if (directory)
            {
                if (entry.Length != 0) throw new InvalidDataException("Nonempty directory entry.");
            }
            else entries.Add(path, entry);
        }
        Dictionary<string, string> canonicalDirectories = new(StringComparer.OrdinalIgnoreCase);
        foreach (string path in allPaths)
        {
            for (int i = path.IndexOf('/'); i >= 0; i = path.IndexOf('/', i + 1))
            {
                string parent = path[..i];
                if (entries.ContainsKey(parent)) throw new InvalidDataException("File/directory path collision.");
                if (canonicalDirectories.TryGetValue(parent, out string? prior) && prior != parent)
                    throw new InvalidDataException("Directory case collision.");
                canonicalDirectories[parent] = parent;
            }
            if (!entries.ContainsKey(path))
            {
                if (canonicalDirectories.TryGetValue(path, out string? prior) && prior != path)
                    throw new InvalidDataException("Directory case collision.");
                canonicalDirectories[path] = path;
            }
        }
        if (!entries.Remove("manifest.json", out ZipArchiveEntry? meta) || meta.FullName != "manifest.json")
            throw new InvalidDataException("Missing root manifest.json.");
        using MemoryStream manifestData = new();
        using (Stream stream = meta.Open()) _ = CopyAndHash(stream, manifestData, 256 * 1024, out _);
        PluginManifest manifest = PluginManifest.Parse(manifestData.ToArray());
        if (entries.Count != manifest.Files.Count) throw new InvalidDataException("Manifest file count differs.");
        long total = manifestData.Length;
        foreach (var pair in entries.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!manifest.Files.TryGetValue(pair.Key, out string? expected)) throw new InvalidDataException("Unlisted entry: " + pair.Key);
            long limit = Limit(pair.Key, manifest.IconPath);
            if (pair.Value.Length > limit) throw new InvalidDataException("Entry too large.");
            // Spool the entry DLL to disk only in extraction; verify-only uses a bounded memory stream for PE headers.
            using Stream source = pair.Value.Open();
            bool isEntry = pair.Key.Equals(manifest.Entry, StringComparison.OrdinalIgnoreCase);
            string? destination = workspace is null ? null : PackagePaths.Resolve(workspace, pair.Key);
            if (destination is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                PackagePaths.RejectReparseAncestors(destination);
            }
            using Stream target = destination is not null
                ? new FileStream(destination, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None)
                : isEntry ? new MemoryStream() : Stream.Null;
            string actual = CopyAndHash(source, target, Math.Min(limit, MaxExpanded - total), out long length);
            total += length;
            if (length != pair.Value.Length || !actual.Equals(expected, StringComparison.Ordinal))
                throw new InvalidDataException("Entry hash or length mismatch: " + pair.Key);
            if (isEntry) { target.Position = 0; ValidateNativeDll(target); }
        }
        return manifest;
    }

    private static long Limit(string path, string? icon) => path.EndsWith(".hxaml", StringComparison.OrdinalIgnoreCase) ? MiB
        : path.Equals(icon, StringComparison.OrdinalIgnoreCase) ? 4 * MiB : MaxEntry;

    private static ZipArchiveEntry CreateEntry(ZipArchive archive, string path)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        entry.LastWriteTime = Epoch;
        entry.ExternalAttributes = 0;
        return entry;
    }

    private static string CopyAndHash(Stream input, Stream output, long limit, out long length)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[64 * 1024];
        length = 0;
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
        {
            length += read;
            if (length > limit) throw new InvalidDataException("Expanded size limit exceeded.");
            hash.AppendData(buffer, 0, read);
            output.Write(buffer, 0, read);
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static string Hash(Stream input) => Convert.ToHexStringLower(SHA256.HashData(input));

    private static void ValidateNativeDll(Stream stream)
    {
        using PEReader reader = new(stream, PEStreamOptions.LeaveOpen);
        if (reader.PEHeaders.CoffHeader.Machine != Machine.Amd64 ||
            (reader.PEHeaders.CoffHeader.Characteristics & Characteristics.Dll) == 0 ||
            reader.PEHeaders.PEHeader is null || reader.PEHeaders.CorHeader is not null)
            throw new InvalidDataException("Entry must be a native x64 PE DLL, not a managed Build output.");
    }
}
