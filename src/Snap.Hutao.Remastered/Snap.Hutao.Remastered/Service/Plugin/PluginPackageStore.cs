using System.IO;
using System.IO.Compression;
using Snap.Hutao.Remastered.API.Model;
using Snap.Hutao.Remastered.Core;

namespace Snap.Hutao.Remastered.Service.Plugin;

/// <summary>
/// The package files a plugin is installed as: where they live, and how an archive is described and unpacked.
/// The durable state of a plugin is exactly one file, so enabling and disabling is one move between the two
/// locations and nothing here can leave a plugin half written.
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IPluginPackageStore))]
internal sealed partial class PluginPackageStore : IPluginPackageStore
{
    private const string EnabledExtension = ".hutao";
    private const string DisabledExtension = ".hutaodisabled";

    [GeneratedConstructor]
    public partial PluginPackageStore(IServiceProvider serviceProvider);

    public string PluginsDirectory => Path.Combine(HutaoRuntime.DataDirectory, "Plugins");

    public bool IsPackageFile(string path)
    {
        string extension = Path.GetExtension(path);
        return extension is EnabledExtension or DisabledExtension;
    }

    /// <summary>
    /// The source extension decides the installed state: picking a <c>.hutaodisabled</c> package installs a
    /// disabled plugin instead of silently enabling it.
    /// </summary>
    public (bool IsEnabled, string TargetPath) ResolveInstallTarget(string pluginId, string sourcePath)
    {
        string extension = Path.GetExtension(sourcePath);
        return extension switch
        {
            EnabledExtension => (true, Path.Combine(PluginsDirectory, $"{pluginId}{extension}")),
            DisabledExtension => (false, Path.Combine(PluginsDirectory, $"{pluginId}{extension}")),
            _ => (false, string.Empty),
        };
    }

    public string GetPackagePath(string pluginId, bool enabled)
    {
        return Path.Combine(PluginsDirectory, $"{pluginId}{(enabled ? EnabledExtension : DisabledExtension)}");
    }

    public bool IsEnabled(string pluginId)
    {
        return File.Exists(GetPackagePath(pluginId, enabled: true));
    }

    /// <summary>
    /// The enabled location wins: a pair of packages that both survived a crash still resolves to the one the
    /// loader has to use.
    /// </summary>
    public (bool IsEnabled, string PackagePath) Locate(string pluginId)
    {
        string enabledPath = GetPackagePath(pluginId, enabled: true);
        if (File.Exists(enabledPath))
        {
            return (true, enabledPath);
        }

        string disabledPath = GetPackagePath(pluginId, enabled: false);
        return File.Exists(disabledPath) ? (false, disabledPath) : (false, string.Empty);
    }

    /// <summary>
    /// A plugin that has both files is yielded twice on purpose, so the second load reports the conflict instead
    /// of the enumeration hiding it.
    /// </summary>
    public IEnumerable<string> EnumerateInstalledPluginIds()
    {
        if (!Directory.Exists(PluginsDirectory))
        {
            yield break;
        }

        foreach (string packagePath in Directory.GetFiles(PluginsDirectory, $"*{EnabledExtension}"))
        {
            yield return Path.GetFileNameWithoutExtension(packagePath);
        }

        foreach (string packagePath in Directory.GetFiles(PluginsDirectory, $"*{DisabledExtension}"))
        {
            yield return Path.GetFileNameWithoutExtension(packagePath);
        }
    }

    public void CopyPackage(string sourcePath, string targetPath)
    {
        Directory.CreateDirectory(PluginsDirectory);
        File.Copy(sourcePath, targetPath);
    }

    public void MoveToEnabled(string pluginId)
    {
        string disabledPath = GetPackagePath(pluginId, enabled: false);
        if (!File.Exists(disabledPath))
        {
            return;
        }

        File.Move(disabledPath, GetPackagePath(pluginId, enabled: true));
    }

    public void MoveToDisabled(string pluginId)
    {
        File.Move(GetPackagePath(pluginId, enabled: true), GetPackagePath(pluginId, enabled: false));
    }

    public void DeletePackages(string pluginId)
    {
        File.Delete(GetPackagePath(pluginId, enabled: true));
        File.Delete(GetPackagePath(pluginId, enabled: false));
    }

    /// <summary>
    /// Describes and unpacks in one pass, so a package replaced in between cannot be described by one file and
    /// unpacked from another.
    /// </summary>
    public async ValueTask<PluginManifest?> UnpackAsync(string packagePath, string targetDirectory)
    {
        using ZipArchive archive = await ZipFile.OpenReadAsync(packagePath).ConfigureAwait(false);

        PluginManifest? manifest = await ReadManifestAsync(archive).ConfigureAwait(false);
        if (manifest is null)
        {
            return null;
        }

        archive.ExtractToDirectory(targetDirectory, overwriteFiles: true);
        return manifest;
    }

    public void Unpack(string packagePath, string targetDirectory)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        archive.ExtractToDirectory(targetDirectory, overwriteFiles: true);
    }

    public async ValueTask<PluginManifest?> ReadManifestAsync(string packagePath)
    {
        using ZipArchive archive = await ZipFile.OpenReadAsync(packagePath).ConfigureAwait(false);
        return await ReadManifestAsync(archive).ConfigureAwait(false);
    }

    private static async ValueTask<PluginManifest?> ReadManifestAsync(ZipArchive archive)
    {
        ZipArchiveEntry? manifestEntry = archive.GetEntry("manifest.json");
        if (manifestEntry is null)
        {
            return null;
        }

        using Stream manifestStream = manifestEntry.Open();
        return await JsonSerializer.DeserializeAsync<PluginManifest>(manifestStream).ConfigureAwait(false);
    }
}
