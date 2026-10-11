using Snap.Hutao.Remastered.API.Model;

namespace Snap.Hutao.Remastered.Service.Plugin;

public interface IPluginService
{
    /// <summary>
    /// Raised on the main thread whenever the set of installed plugins changed. Plugins are loaded
    /// during background initialization, long after the plugin page may already be open, so a view that
    /// lists them has to be told to refresh instead of reading the registry once.
    /// </summary>
    event EventHandler? PluginsChanged;

    Task<bool> InstallPluginAsync(string path);

    /// <summary>
    /// Loads a plugin. An enabled plugin is loaded and enabled; a disabled plugin is only described,
    /// so it stays visible in the plugin manager without its assemblies being loaded.
    /// </summary>
    Task<bool> LoadPluginAsync(string id, bool suppressNotification = false);

    /// <summary>
    /// Loads every plugin found in the plugins directory, and refreshes the descriptions of the ones
    /// that are already tracked.
    /// </summary>
    Task LoadAllPluginsAsync();

    Task<bool> EnablePluginAsync(string id, bool suppressNotification = false);

    Task<bool> DisablePluginAsync(string id, bool suppressNotification = false);

    /// <summary>
    /// Releases the assemblies of a plugin without touching the plugin files. The plugin keeps its
    /// description, so it stays listed as disabled.
    /// </summary>
    void UnloadPlugin(string id);

    void UninstallPlugin(string id);

    bool TryGetPluginLoadContext(string id, [NotNullWhen(true)] out WeakReference? loadContextReference);

    PluginInfo? GetPluginInfoById(string id);

    /// <summary>
    /// Every installed plugin, enabled or disabled.
    /// </summary>
    IReadOnlyList<PluginInfo> GetAllPluginInfos();

    /// <summary>
    /// The live plugin objects, enabled plugins only.
    /// </summary>
    IReadOnlyList<HutaoPlugin> GetAllPlugins();

    string? GetPluginPath(string id);

    string GetPluginsDirectory();

    IPluginSettingService GetSettingService();
}
