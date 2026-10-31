using Snap.Hutao.Remastered.API.Model;

namespace Snap.Hutao.Remastered.Service.Plugin;

internal interface IPluginPackageStore
{
    string PluginsDirectory { get; }

    bool IsPackageFile(string path);

    bool IsEnabled(string pluginId);

    string GetPackagePath(string pluginId, bool enabled);

    (bool IsEnabled, string PackagePath) Locate(string pluginId);

    IEnumerable<string> EnumerateInstalledPluginIds();

    (bool IsEnabled, string TargetPath) ResolveInstallTarget(string pluginId, string sourcePath);

    void CopyPackage(string sourcePath, string targetPath);

    void MoveToEnabled(string pluginId);

    void MoveToDisabled(string pluginId);

    void DeletePackages(string pluginId);

    ValueTask<PluginManifest?> ReadManifestAsync(string packagePath);

    ValueTask<PluginManifest?> UnpackAsync(string packagePath, string targetDirectory);

    void Unpack(string packagePath, string targetDirectory);
}
