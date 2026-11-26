using System.Text.RegularExpressions;

namespace Snap.Hutao.Plugin.Packaging;

public static class PackagePaths
{
    public static void ValidateIdentifier(string value)
    {
        if (value.Length is < 1 or > 128 || !char.IsAsciiLetterOrDigit(value[0]) ||
            value.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '_' or '-')) ||
            value.EndsWith('.') || value.Contains("..", StringComparison.Ordinal))
            throw new InvalidDataException("Invalid identifier: " + value);
        ValidateSegment(value);
    }

    private static void ValidateSegment(string segment)
    {
        if (segment.Length is < 1 or > 255 || segment is "." or ".." ||
            segment.EndsWith(' ') || segment.EndsWith('.') ||
            segment.Any(c => c < 32 || c is '\\' or ':' or '<' or '>' or '"' or '|' or '?' or '*'))
            throw new InvalidDataException("Invalid package path segment: " + segment);
        string device = segment.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
        if (device is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" ||
            Regex.IsMatch(device, @"^(COM|LPT)[1-9¹²³]$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("Reserved device name: " + segment);
    }

    public static void ValidateRelativePath(string path)
    {
        if (path.Length is < 1 or > 1024) throw new InvalidDataException("Invalid package path length.");
        foreach (string segment in path.Split('/')) ValidateSegment(segment);
    }

    public static string Resolve(string root, string relativePath)
    {
        ValidateRelativePath(relativePath);
        string fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        string fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Path escapes package.");
        return fullPath;
    }

    public static void RejectReparseAncestors(string path)
    {
        for (string? current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Reparse point is not allowed: " + current);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }
}
