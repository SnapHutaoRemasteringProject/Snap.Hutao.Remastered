using System.IO;
using Snap.Hutao.Remastered.API.Model;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Service.Notification;

namespace Snap.Hutao.Remastered.Service.Plugin;

/// <summary>
/// The plugin state machine: install, load, enable, disable and uninstall. It owns the order of a transition —
/// the plugin's own hook first, then the package file, then the visible state — plus the user-facing feedback
/// for it. Package files are <see cref="IPluginPackageStore"/>'s, unpacked directories
/// <see cref="IPluginWorkspace"/>'s, assemblies <see cref="IPluginAssemblyLoader"/>'s and the catalog
/// <see cref="IPluginRegistry"/>'s.
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IPluginService))]
public partial class PluginService : IPluginService
{
    private readonly ILogger<PluginService> logger;
    private readonly IMessenger messenger;
    private readonly IServiceProvider serviceProvider;
    private readonly IPluginPackageStore packageStore;
    private readonly IPluginWorkspace workspace;
    private readonly IPluginAssemblyLoader assemblyLoader;
    private readonly IPluginRegistry registry;

    [GeneratedConstructor]
    public partial PluginService(IServiceProvider serviceProvider);

    public event EventHandler? PluginsChanged
    {
        add => registry.Changed += value;
        remove => registry.Changed -= value;
    }

    public string PluginsDirectory => packageStore.PluginsDirectory;

    public async Task<bool> DisablePluginAsync(string id, bool suppressNotification = false)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Disabling plugin", category: "PluginService", data: new Dictionary<string, string>
        {
            { "PluginId", id },
        }));

        try
        {
            if (registry.GetInfo(id) is not { } pluginInfo)
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginNotFound(id)));
                return false;
            }

            if (!packageStore.IsEnabled(id))
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginNotFound(pluginInfo.Manifest.Name)));
                return false;
            }

            // The plugin's own hook is the only step that runs third-party code and the only step that cannot be
            // undone, so it runs before anything is committed. A throwing OnDisable then leaves the plugin enabled
            // on disk and loaded in memory, which is exactly what the manager shows.
            pluginInfo.Instance?.OnDisable();

            // Commit: package move, then the visible flag, then the assemblies.
            packageStore.MoveToDisabled(id);
            await registry.SetEnabledAsync(pluginInfo, isEnabled: false);

            // The plugin code must go away with the plugin; the description survives, which is what keeps the
            // disabled plugin listed in the plugin manager.
            UnloadPlugin(id);

            if (!suppressNotification)
            {
                messenger.Send(InfoBarMessage.Success(SH.FormatServicePluginDisableSuccess(pluginInfo.Manifest.Name)));
            }

            return true;
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(SH.ServicePluginDisablingPluginFailed, ex));
            return false;
        }
    }

    public async Task<bool> EnablePluginAsync(string id, bool suppressNotification = false)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Enabling plugin", category: "PluginService", data: new Dictionary<string, string>
        {
            { "PluginId", id },
        }));

        try
        {
            if (registry.GetInfo(id) is not { } pluginInfo)
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginNotFound(id)));
                return false;
            }

            // Making the plugin object exist is not a commit on its own: the package still says "disabled" until
            // the hook below succeeded, and the catch releases a freshly loaded instance again.
            bool loadedHere = pluginInfo.Instance is null;
            if (loadedHere && !await LoadPluginInstanceAsync(pluginInfo).ConfigureAwait(false))
            {
                return false;
            }

            try
            {
                // Hook before commit, for the same reason as in DisablePluginAsync: OnEnable is third-party code,
                // so a throw must not leave a package enabled on disk while the manager reports it as disabled.
                pluginInfo.Instance?.OnEnable();
                packageStore.MoveToEnabled(id);
            }
            catch
            {
                if (loadedHere)
                {
                    UnloadPlugin(id);
                }

                throw;
            }

            await registry.SetEnabledAsync(pluginInfo, isEnabled: true);

            if (!suppressNotification)
            {
                messenger.Send(InfoBarMessage.Success(SH.FormatServicePluginEnablingSuccess(pluginInfo.Manifest.Name)));
            }

            return true;
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(SH.ServicePluginEnablingPluginFailed, ex));
            return false;
        }
    }

    public async Task<bool> LoadPluginAsync(string id, bool suppressNotification = false)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Loading plugin", category: "PluginService", data: new Dictionary<string, string>
        {
            { "PluginId", id },
        }));

        try
        {
            if (registry.GetInfo(id) is { } existing)
            {
                // Already enabled: re-enabling is what toggling the state back on means.
                return existing.IsEnabled
                    ? await EnablePluginAsync(id, suppressNotification).ConfigureAwait(false)
                    : await DescribeDisabledPluginAsync(id).ConfigureAwait(false);
            }

            return await LoadPluginAsyncCore(id, suppressNotification).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(SH.ServicePluginLoadFailed, ex));
            return false;
        }
    }

    public async Task LoadAllPluginsAsync()
    {
        await workspace.CleanupStaleDirectoriesAsync().ConfigureAwait(false);

        try
        {
            foreach (string pluginId in packageStore.EnumerateInstalledPluginIds())
            {
                await LoadPluginAsync(pluginId, suppressNotification: true).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enumerate plugins in '{Directory}'", packageStore.PluginsDirectory);
            SentrySdk.CaptureException(ex);
        }

        // Always notify: a page opened while this scan was still running has to be refreshed even when every plugin
        // failed to load, otherwise it keeps showing the empty state forever.
        registry.RaiseChanged();
    }

    public void UnloadPlugin(string id)
    {
        if (registry.TakeLoadContext(id) is { } loadContext)
        {
            assemblyLoader.Unload(id, loadContext);
        }

        if (registry.Detach(id) is { } pluginDirectory)
        {
            workspace.DeleteDirectory(pluginDirectory);
        }
    }

    public void UninstallPlugin(string id)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Uninstalling plugin", category: "PluginService", data: new Dictionary<string, string>
        {
            { "PluginId", id },
        }));

        try
        {
            registry.GetInfo(id)?.Instance?.OnUninstall();

            packageStore.DeletePackages(id);
            UnloadPlugin(id);
            registry.Remove(id);

            messenger.Send(InfoBarMessage.Success(SH.FormatServicePluginDeleteSuccess(id)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to uninstall plugin '{PluginId}'", id);
            SentrySdk.CaptureException(ex);
            messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginDeleteFailed(id), ex));
        }
    }

    public async Task<bool> InstallPluginAsync(string path)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Installing plugin", category: "PluginService", data: new Dictionary<string, string>
        {
            { "SourcePath", path },
        }));

        try
        {
            if (!File.Exists(path))
            {
                messenger.Send(InfoBarMessage.Error(SH.ServicePluginInstallFileNotFound));
                return false;
            }

            if (!packageStore.IsPackageFile(path))
            {
                messenger.Send(InfoBarMessage.Error(SH.ServicePluginInstallInvalidFileType));
                return false;
            }

            PluginManifest? manifest = await packageStore.ReadManifestAsync(path).ConfigureAwait(false);
            if (manifest is null || string.IsNullOrEmpty(manifest.Id))
            {
                messenger.Send(InfoBarMessage.Error(SH.ServicePluginInstallInvalidManifest));
                return false;
            }

            string pluginId = manifest.Id;

            (bool isEnabledPackage, string targetPath) = packageStore.ResolveInstallTarget(pluginId, path);
            if (File.Exists(targetPath))
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginInstallAlreadyExists(manifest.Name)));
                return false;
            }

            packageStore.CopyPackage(path, targetPath);

            if (!await LoadPluginAsync(pluginId).ConfigureAwait(false))
            {
                return false;
            }

            if (isEnabledPackage && registry.GetInfo(pluginId) is { Instance: { } installedPlugin })
            {
                installedPlugin.OnInstall();
            }

            messenger.Send(InfoBarMessage.Success(SH.FormatServicePluginInstallSuccess(manifest.Name)));
            return true;
        }
        catch (Exception ex)
        {
            messenger.Send(InfoBarMessage.Error(SH.ServicePluginInstallFailed, ex));
            return false;
        }
    }

    public PluginInfo? GetPluginInfoById(string id)
    {
        return registry.GetInfo(id);
    }

    public IReadOnlyList<PluginInfo> GetAllPluginInfos()
    {
        return registry.GetAllInfos();
    }

    public IReadOnlyList<HutaoPlugin> GetAllPlugins()
    {
        return registry.GetAllPlugins();
    }

    public bool TryGetPluginLoadContext(string id, [NotNullWhen(true)] out WeakReference? loadContextReference)
    {
        return registry.TryGetLoadContextReference(id, out loadContextReference);
    }

    public string? GetPluginPath(string id)
    {
        return registry.GetPath(id);
    }

    public string GetPluginsDirectory()
    {
        return packageStore.PluginsDirectory;
    }

    public IPluginSettingService GetSettingService()
    {
        return serviceProvider.GetRequiredService<IPluginSettingService>();
    }

    private async Task<bool> LoadPluginAsyncCore(string id, bool suppressNotification)
    {
        (bool isEnabled, string packagePath) = packageStore.Locate(id);
        if (packagePath.Length == 0)
        {
            messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginLoadDirectoryNotFound(id)));
            return false;
        }

        string pluginDirectory = workspace.CreateDirectory(id);
        bool succeeded = false;
        PluginAssemblyLoadContext? loadContext = null;

        try
        {
            PluginManifest? manifest = await packageStore.UnpackAsync(packagePath, pluginDirectory).ConfigureAwait(false);
            if (manifest is null)
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginLoadInvalidManifest(id)));
                return false;
            }

            HutaoPlugin? plugin = null;

            if (isEnabled)
            {
                (plugin, loadContext) = assemblyLoader.Load(id, pluginDirectory, manifest);

                string iconPath = Path.Combine(pluginDirectory, "icon.png");
                if (File.Exists(iconPath))
                {
                    plugin.IconPath = iconPath;
                }

                plugin.OnLoad(new PluginContext(serviceProvider));
            }

            await registry.DescribeAsync(id, manifest, pluginDirectory, plugin, isEnabled).ConfigureAwait(false);

            if (plugin is not null && loadContext is not null)
            {
                registry.Attach(id, plugin, loadContext, pluginDirectory);

                // The package was already in its enabled location, so unlike EnablePluginAsync there is no file to
                // move and nothing that can fail; only the plugin's own hook is left to run.
                plugin.OnEnable();
            }

            if (!suppressNotification)
            {
                messenger.Send(InfoBarMessage.Success(SH.FormatServicePluginLoadSuccess(manifest.Name)));
            }

            succeeded = true;
            return true;
        }
        finally
        {
            // Every path that does not end with a usable, described plugin has to release both the context and the
            // unpacked directory, otherwise they survive for the process lifetime.
            if (!succeeded)
            {
                registry.Remove(id);

                if (loadContext is not null)
                {
                    assemblyLoader.Unload(id, loadContext);
                }

                workspace.DeleteDirectory(pluginDirectory);
            }
        }
    }

    /// <summary>
    /// Loads the assembly of a described but currently unloaded plugin, so it can be enabled.
    /// </summary>
    private async Task<bool> LoadPluginInstanceAsync(PluginInfo pluginInfo)
    {
        string id = pluginInfo.Id;
        (_, string packagePath) = packageStore.Locate(id);
        if (packagePath.Length == 0)
        {
            messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginLoadDirectoryNotFound(id)));
            return false;
        }

        string pluginDirectory = workspace.CreateDirectory(id);
        PluginAssemblyLoadContext? loadContext = null;

        try
        {
            packageStore.Unpack(packagePath, pluginDirectory);

            HutaoPlugin plugin;
            (plugin, loadContext) = assemblyLoader.Load(id, pluginDirectory, pluginInfo.Manifest);

            string iconPath = Path.Combine(pluginDirectory, "icon.png");
            if (File.Exists(iconPath))
            {
                plugin.IconPath = iconPath;
            }

            plugin.OnLoad(new PluginContext(serviceProvider));

            registry.Attach(id, plugin, loadContext, pluginDirectory);
            await registry.SetIconPathAsync(pluginInfo, plugin.IconPath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A failed enable must not leave a context, an instance entry or an unpacked tree behind. The
            // description is kept: the plugin is still installed, and still disabled.
            registry.Detach(id);

            if (loadContext is not null)
            {
                assemblyLoader.Unload(id, loadContext);
            }

            workspace.DeleteDirectory(pluginDirectory);

            logger.LogError(ex, "Failed to load plugin '{PluginId}'", id);
            SentrySdk.CaptureException(ex);
            messenger.Send(InfoBarMessage.Error(SH.ServicePluginLoadFailed, ex));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Refreshes a tracked disabled plugin from its package. The assemblies stay unloaded; only the description
    /// is read, which is what the plugin manager lists.
    /// </summary>
    private async Task<bool> DescribeDisabledPluginAsync(string id)
    {
        string packagePath = packageStore.GetPackagePath(id, enabled: false);
        if (!File.Exists(packagePath))
        {
            messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginLoadDirectoryNotFound(id)));
            return false;
        }

        string pluginDirectory = workspace.CreateDirectory(id);
        bool succeeded = false;

        try
        {
            PluginManifest? manifest = await packageStore.UnpackAsync(packagePath, pluginDirectory).ConfigureAwait(false);
            if (manifest is null)
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginLoadInvalidManifest(id)));
                return false;
            }

            await registry.DescribeAsync(id, manifest, pluginDirectory, instance: null, isEnabled: false).ConfigureAwait(false);

            succeeded = true;
            return true;
        }
        finally
        {
            if (!succeeded)
            {
                workspace.DeleteDirectory(pluginDirectory);
            }
        }
    }
}
