using System.IO;
using Snap.Hutao.Remastered.API.Model;

namespace Snap.Hutao.Remastered.Service.Plugin;

/// <summary>
/// The in-memory catalog of installed plugins: the descriptions the plugin manager lists, the live objects of
/// the enabled ones, the unpacked directory each plugin occupies and the load context that owns its assemblies.
/// All of it is one unit behind one lock, so no caller can observe a plugin whose description and assemblies
/// disagree. <see cref="PluginService"/> decides when a plugin changes; this type decides how the change becomes
/// visible, which is why the observable properties of the descriptions and the changed notification are only
/// ever published on the main thread.
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IPluginRegistry))]
internal sealed partial class PluginRegistry : IPluginRegistry
{
    // Guards plugins, infos, directories and loadContexts as one unit.
    private readonly object stateLock = new();

    /// <summary>
    /// Live plugin objects. Only enabled plugins appear here: loading an assembly is what makes a plugin a
    /// <see cref="HutaoPlugin"/>, and unloading is what removes it again.
    /// </summary>
    private readonly List<HutaoPlugin> plugins = new();

    /// <summary>
    /// Every installed plugin, including the disabled ones that have no loaded assembly.
    /// </summary>
    private readonly Dictionary<string, PluginInfo> infos = new();

    private readonly Dictionary<string, string> directories = new();
    private readonly Dictionary<string, PluginAssemblyLoadContext> loadContexts = new();

    private readonly ITaskContext taskContext;

    [GeneratedConstructor]
    public partial PluginRegistry(IServiceProvider serviceProvider);

    public event EventHandler? Changed;

    public void RaiseChanged()
    {
        taskContext.BeginInvokeOnMainThread(NotifyChanged);
    }

    public PluginInfo? GetInfo(string id)
    {
        lock (stateLock)
        {
            return infos.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<PluginInfo> GetAllInfos()
    {
        lock (stateLock)
        {
            return infos.Values
                .OrderByDescending(info => info.IsEnabled)
                .ThenBy(info => info.Manifest.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<HutaoPlugin> GetAllPlugins()
    {
        lock (stateLock)
        {
            return plugins.ToList();
        }
    }

    public string? GetPath(string id)
    {
        lock (stateLock)
        {
            if (directories.TryGetValue(id, out string? path))
            {
                return path;
            }
        }

        return null;
    }

    public bool TryGetLoadContextReference(string id, [NotNullWhen(true)] out WeakReference? loadContextReference)
    {
        lock (stateLock)
        {
            if (loadContexts.TryGetValue(id, out PluginAssemblyLoadContext? loadContext))
            {
                loadContextReference = new WeakReference(loadContext);
                return true;
            }
        }

        loadContextReference = null;
        return false;
    }

    /// <summary>
    /// Creates the description on first load and refreshes its content when the package was replaced, without
    /// discarding the live instance of an enabled plugin.
    /// </summary>
    public async ValueTask<PluginInfo> DescribeAsync(string id, PluginManifest manifest, string pluginDirectory, HutaoPlugin? instance, bool isEnabled)
    {
        string iconPath = Path.Combine(pluginDirectory, "icon.png");
        if (!File.Exists(iconPath))
        {
            iconPath = string.Empty;
        }

        PluginInfo pluginInfo;

        lock (stateLock)
        {
            if (infos.TryGetValue(id, out PluginInfo? existing))
            {
                // Manifest and Instance are plain properties, so refreshing them under the lock is safe; the
                // observable IconPath is applied on the main thread below.
                existing.Manifest = manifest;
                existing.Instance = instance ?? existing.Instance;
                pluginInfo = existing;
            }
            else
            {
                // The description is not published until the end of this block, so its observable properties
                // can be initialized here without notifying a binding.
                pluginInfo = new()
                {
                    Id = id,
                    Manifest = manifest,
                    IconPath = iconPath,
                    IsEnabled = isEnabled,
                    Instance = instance,
                };

                infos[id] = pluginInfo;
                directories[id] = pluginDirectory;
            }
        }

        await SetIconPathAsync(pluginInfo, iconPath).ConfigureAwait(false);
        RaiseChanged();
        return pluginInfo;
    }

    public async ValueTask SetEnabledAsync(PluginInfo pluginInfo, bool isEnabled)
    {
        if (pluginInfo.IsEnabled == isEnabled)
        {
            return;
        }

        await taskContext.SwitchToMainThreadAsync();
        pluginInfo.IsEnabled = isEnabled;
    }

    public async ValueTask SetIconPathAsync(PluginInfo pluginInfo, string iconPath)
    {
        if (string.Equals(pluginInfo.IconPath, iconPath, StringComparison.Ordinal))
        {
            return;
        }

        await taskContext.SwitchToMainThreadAsync();
        pluginInfo.IconPath = iconPath;
    }

    public void Attach(string id, HutaoPlugin plugin, PluginAssemblyLoadContext loadContext, string pluginDirectory)
    {
        lock (stateLock)
        {
            plugins.Add(plugin);
            loadContexts[id] = loadContext;
            directories[id] = pluginDirectory;

            if (infos.TryGetValue(id, out PluginInfo? pluginInfo))
            {
                pluginInfo.Instance = plugin;
            }
        }
    }

    /// <summary>
    /// Hands the context to the caller and stops tracking it, so exactly one owner can unload it.
    /// </summary>
    public PluginAssemblyLoadContext? TakeLoadContext(string id)
    {
        lock (stateLock)
        {
            return loadContexts.Remove(id, out PluginAssemblyLoadContext? loadContext) ? loadContext : null;
        }
    }

    /// <summary>
    /// Releases the in-memory part of a plugin that is going away while keeping its description, which is what
    /// leaves a disabled plugin listed in the plugin manager, and returns the directory the caller has to
    /// reclaim.
    /// </summary>
    public string? Detach(string id)
    {
        string? pluginDirectory;

        lock (stateLock)
        {
            plugins.RemoveAll(plugin => plugin.Manifest.Id == id);

            if (infos.TryGetValue(id, out PluginInfo? pluginInfo))
            {
                pluginInfo.Instance = null;
            }

            directories.TryGetValue(id, out pluginDirectory);
            RaiseChanged();
        }

        return pluginDirectory;
    }

    /// <summary>
    /// Drops a plugin from the catalog after its packages were deleted or its first load failed. The live
    /// assemblies must already be gone: a tracked context is forgotten here without being unloaded, so whoever
    /// took it — or created it — stays the only one responsible for releasing it.
    /// </summary>
    public void Remove(string id)
    {
        lock (stateLock)
        {
            plugins.RemoveAll(plugin => plugin.Manifest.Id == id);
            infos.Remove(id);
            directories.Remove(id);
            loadContexts.Remove(id);
            RaiseChanged();
        }
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
