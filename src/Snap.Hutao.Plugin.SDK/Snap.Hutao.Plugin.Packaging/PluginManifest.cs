using System.Text.Json;

namespace Snap.Hutao.Plugin.Packaging;

public sealed record PluginManifest(string Id, string Name, Version Version, IReadOnlyList<string> Author, string Description,
    string? IconPath, Version MinHostVersion, uint AbiMajor, uint MinMinor, string Entry, string UiMode,
    IReadOnlySet<string> Capabilities, IReadOnlyDictionary<string, string> Files, string? MetadataProvider = null)
{
    private static readonly HashSet<string> KnownCapabilities = new(StringComparer.Ordinal)
        { "settings", "notifications", "navigation", "commands", "dialogs", "xaml" };

    public static PluginManifest Parse(ReadOnlyMemory<byte> bytes, bool template = false)
    {
        if (bytes.Length > 256 * 1024) throw new InvalidDataException("Manifest too large.");
        if (bytes.Span.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) bytes = bytes[3..];
        using JsonDocument document = JsonDocument.Parse(bytes, new() { MaxDepth = 16 });
        JsonElement root = document.RootElement;
        RejectDuplicates(root);
        if (root.GetProperty("schemaVersion").GetInt32() != 2) throw new InvalidDataException("Unsupported plugin schema.");
        string id = ReadString(root, "id", 128, true);
        PackagePaths.ValidateIdentifier(id);
        string name = ReadString(root, "name", 128, true);
        string description = ReadString(root, "description", 4096, false);
        JsonElement version = root.GetProperty("version");
        Version pluginVersion = new(version.GetProperty("major").GetInt32(), version.GetProperty("minor").GetInt32(), version.GetProperty("patch").GetInt32());
        string[] authors = root.GetProperty("author").EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
        if (authors.Length is < 1 or > 16 || authors.Any(x => x.Length is < 1 or > 128)) throw new InvalidDataException("Invalid authors.");
        string hostVersion = ReadString(root, "minHostVersion", 64, true);
        if (!Version.TryParse(hostVersion, out Version? minimum) || minimum.Build < 0 || minimum.Revision != -1)
            throw new InvalidDataException("minHostVersion must have three components.");
        JsonElement api = root.GetProperty("api");
        uint major = api.GetProperty("major").GetUInt32(), minor = api.GetProperty("minMinor").GetUInt32();
        if (major != 1 || minor != 0) throw new InvalidDataException("Incompatible ABI.");
        JsonElement runtime = root.GetProperty("runtime");
        if (ReadString(runtime, "kind", 32, true) != "winrt-native" || ReadString(runtime, "architecture", 16, true) != "x64")
            throw new InvalidDataException("Only x64 WinRT-native plugins are supported.");
        string entry = ReadString(runtime, "entry", 1024, true);
        PackagePaths.ValidateRelativePath(entry);
        if (!entry.StartsWith("bin/x64/", StringComparison.Ordinal) || !entry.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Entry must be an x64 DLL under bin/x64.");
        string mode = ReadString(root.GetProperty("ui"), "mode", 32, true);
        if (mode is not ("none" or "loose" or "programmatic" or "compiled")) throw new InvalidDataException("Unsupported UI mode.");
        // Compiled UI packages can be inspected/packed; activation requires explicit host opt-in.
        string? provider = null;
        if (mode == "compiled")
        {
            provider = ReadString(root.GetProperty("ui"), "metadataProvider", 256, true);
            string[] parts = provider.Split('.');
            if (parts.Length < 2 || parts.Any(part => part.Length == 0 || !(char.IsAsciiLetter(part[0]) || part[0] == '_') ||
                part.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_')))
                throw new InvalidDataException("Invalid XAML metadata provider runtimeclass.");
        }
        HashSet<string> capabilities = new(StringComparer.Ordinal);
        foreach (JsonElement item in root.GetProperty("capabilities").EnumerateArray())
        {
            string capability = item.GetString() ?? "";
            if (!KnownCapabilities.Contains(capability) || !capabilities.Add(capability)) throw new InvalidDataException("Invalid/duplicate capability: " + capability);
        }
        if ((mode == "none" && capabilities.Contains("navigation")) || (mode != "loose" && capabilities.Contains("xaml")))
            throw new InvalidDataException("Inconsistent UI mode and capabilities.");
        string? icon = root.TryGetProperty("iconPath", out var iconElement) ? iconElement.GetString() : null;
        if (icon is not null) PackagePaths.ValidateRelativePath(icon);
        Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
        if (!template)
        {
            foreach (JsonElement item in root.GetProperty("files").EnumerateArray())
            {
                string path = ReadString(item, "path", 1024, true), hash = ReadString(item, "sha256", 64, true);
                PackagePaths.ValidateRelativePath(path);
                if (path.Equals("manifest.json", StringComparison.OrdinalIgnoreCase) || hash.Length != 64 ||
                    hash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) || !files.TryAdd(path, hash))
                    throw new InvalidDataException("Invalid/duplicate manifest file: " + path);
            }
            if (files.Count is < 1 or > 4095 || !files.ContainsKey(entry) || (icon is not null && !files.ContainsKey(icon)))
                throw new InvalidDataException("Manifest entry/icon is missing.");
        }
        return new(id, name, pluginVersion, authors, description, icon, minimum, major, minor, entry, mode, capabilities, files, provider);
    }

    private static string ReadString(JsonElement root, string key, int limit, bool required)
    {
        string value = root.GetProperty(key).GetString() ?? throw new InvalidDataException("Null " + key);
        if (value.Length > limit || (required && value.Length == 0)) throw new InvalidDataException("Invalid " + key);
        return value;
    }

    private static void RejectDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate JSON property: " + property.Name);
                RejectDuplicates(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (JsonElement child in element.EnumerateArray()) RejectDuplicates(child);
    }
}
