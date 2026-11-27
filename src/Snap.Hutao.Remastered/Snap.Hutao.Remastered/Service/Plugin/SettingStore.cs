using System.IO;
using System.Text.Json;
using Snap.Hutao.Plugin.Packaging;

namespace Snap.Hutao.Remastered.Service.Plugin;

internal static class SettingStore
{
    public static Dictionary<string, JsonElement> Read(string directory)
    {
        Dictionary<string, JsonElement> result = new(StringComparer.OrdinalIgnoreCase);
        Directory.CreateDirectory(directory);
        PackagePaths.RejectReparseAncestors(directory);
        foreach (string path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
        {
            PackagePaths.RejectReparseAncestors(path);
            string name = Path.GetFileNameWithoutExtension(path);
            PackagePaths.ValidateIdentifier(name);
            using FileStream input = File.OpenRead(path);
            if (input.Length == 0) continue; // Legacy reset marker.
            if (input.Length > 1024 * 1024) throw new InvalidDataException("Setting exceeds 1 MiB: " + name);
            using JsonDocument document = JsonDocument.Parse(input, new() { MaxDepth = 4 });
            result.Add(name, document.RootElement.Clone());
        }
        return result;
    }

    public static void Write(string directory, string name, object value, bool reset)
    {
        PackagePaths.ValidateIdentifier(name);
        Directory.CreateDirectory(directory);
        PackagePaths.RejectReparseAncestors(directory);
        string target = Path.Combine(directory, name + ".json");
        PackagePaths.RejectReparseAncestors(target);
        string temp = Path.Combine(directory, "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (FileStream output = new(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                if (!reset)
                {
                    using Utf8JsonWriter writer = new(output);
                    switch (value)
                    {
                        case int number: writer.WriteNumberValue(number); break;
                        case bool boolean: writer.WriteBooleanValue(boolean); break;
                        case string text: writer.WriteStringValue(text); break;
                        default: throw new InvalidDataException("Unsupported setting type.");
                    }
                    writer.Flush();
                }
                if (output.Length > 1024 * 1024) throw new InvalidDataException("Setting exceeds 1 MiB.");
                output.Flush(flushToDisk: true);
            }
            File.Move(temp, target, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
