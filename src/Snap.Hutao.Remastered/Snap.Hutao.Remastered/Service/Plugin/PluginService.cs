using System.IO;
using System.IO.Compression;
using System.Reflection;
using Snap.Hutao.Remastered.API.Annotation;
using Snap.Hutao.Remastered.API.Model;
using Snap.Hutao.Remastered.Core;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Core.Logging;
using Snap.Hutao.Remastered.Service.Notification;

namespace Snap.Hutao.Remastered.Service.Plugin;

[Service(ServiceLifetime.Singleton, typeof(IPluginService))]
public partial class PluginService : IPluginService
{
    private const string EnabledExtension = ".hutao";
    private const string DisabledExtension = ".hutaodisabled";

    // Layout used before unpacking became instance scoped; kept only so a stale tree can still be reclaimed.
    private const string LegacyTempDirectoryPrefix = "hutao_plugin_";

    private const string TempRootDirectoryName = "SnapHutaoRemastered";
    private const string TempPluginsDirectoryName = "plugins";

    // Guards plugins, pluginInfos, pluginPaths and pluginLoadContexts as one unit.
    private readonly object pluginStateLock = new();

    /// <summary>
    /// Live plugin objects. Only enabled plugins appear here: loading an assembly is what makes a plugin
    /// a <see cref="HutaoPlugin"/>, and unloading is what removes it again.
    /// </summary>
    private readonly List<HutaoPlugin> plugins = new();

    /// <summary>
    /// Every installed plugin, including the disabled ones that have no loaded assembly.
    /// </summary>
    private readonly Dictionary<string, PluginInfo> pluginInfos = new();

    private readonly Dictionary<string, string> pluginPaths = new();
    private readonly Dictionary<string, PluginAssemblyLoadContext> pluginLoadContexts = new();

    private readonly ILogger<PluginService> logger;
    private readonly IMessenger messenger;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;
    private readonly string instanceTempDirectory = CreateInstanceTempDirectory();

    [GeneratedConstructor]
    public partial PluginService(IServiceProvider serviceProvider);

    public event EventHandler? PluginsChanged;

    public string PluginsDirectory => Path.Combine(HutaoRuntime.DataDirectory, "Plugins");

    public async Task<bool> DisablePluginAsync(string id, bool suppressNotification = false)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Disabling plugin", category: "PluginService", data: new Dictionary<string, string>
        {
            { "PluginId", id },
        }));

        try
        {
            if (GetPluginInfoById(id) is not { } pluginInfo)
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginNotFound(id)));
                return false;
            }

            string enabledPath = GetPluginFilePath(id, EnabledExtension);
            string disabledPath = GetPluginFilePath(id, DisabledExtension);

            if (!File.Exists(enabledPath))
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginNotFound(pluginInfo.Manifest.Name)));
                return false;
            }

            File.Move(enabledPath, disabledPath);

            // Flip the state before unloading: UnloadPlugin raises the change notification, and the list
            // must not observe a disabled plugin that still claims to be enabled.
            pluginInfo.IsEnabled = false;
            pluginInfo.Instance?.OnDisable();

            // The plugin code must go away with the plugin. The description survives, which is what keeps
            // the disabled plugin listed in the plugin manager.
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
            if (GetPluginInfoById(id) is not { } pluginInfo)
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginNotFound(id)));
                return false;
            }

            if (pluginInfo.Instance is null && !await LoadPluginInstanceAsync(pluginInfo).ConfigureAwait(false))
            {
                return false;
            }

            string enabledPath = GetPluginFilePath(id, EnabledExtension);
            string disabledPath = GetPluginFilePath(id, DisabledExtension);

            if (File.Exists(disabledPath))
            {
                File.Move(disabledPath, enabledPath);
            }

            // Flip the state before OnEnable: the hook runs plugin code that may already observe the flag,
            // and a listener must not be told about a disabled plugin's enable hook.
            bool wasEnabled = pluginInfo.IsEnabled;
            pluginInfo.IsEnabled = true;

            try
            {
                pluginInfo.Instance?.OnEnable();
            }
            catch
            {
                pluginInfo.IsEnabled = wasEnabled;
                throw;
            }

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

    public PluginInfo? GetPluginInfoById(string id)
    {
        lock (pluginStateLock)
        {
            return pluginInfos.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<PluginInfo> GetAllPluginInfos()
    {
        lock (pluginStateLock)
        {
            return pluginInfos.Values
                .OrderByDescending(info => info.IsEnabled)
                .ThenBy(info => info.Manifest.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<HutaoPlugin> GetAllPlugins()
    {
        lock (pluginStateLock)
        {
            return plugins.ToList();
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
            Directory.CreateDirectory(PluginsDirectory);

            if (!File.Exists(path))
            {
                messenger.Send(InfoBarMessage.Error(SH.ServicePluginInstallFileNotFound));
                return false;
            }

            string extension = Path.GetExtension(path);
            if (extension != EnabledExtension && extension != DisabledExtension)
            {
                messenger.Send(InfoBarMessage.Error(SH.ServicePluginInstallInvalidFileType));
                return false;
            }

            using ZipArchive archive = await ZipFile.OpenReadAsync(path);

            PluginManifest? manifest = await ReadManifestAsync(archive).ConfigureAwait(false);
            if (manifest is null || string.IsNullOrEmpty(manifest.Id))
            {
                messenger.Send(InfoBarMessage.Error(SH.ServicePluginInstallInvalidManifest));
                return false;
            }

            string pluginId = manifest.Id;
            string targetPath = GetPluginFilePath(pluginId, extension);

            if (File.Exists(targetPath))
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginInstallAlreadyExists(manifest.Name)));
                return false;
            }

            File.Copy(path, targetPath);

            if (!await LoadPluginAsync(pluginId).ConfigureAwait(false))
            {
                return false;
            }

            if (extension == EnabledExtension && GetPluginInfoById(pluginId) is { Instance: { } installedPlugin })
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

    public async Task<bool> LoadPluginAsync(string id, bool suppressNotification = false)
    {
        SentrySdk.AddBreadcrumb(BreadcrumbFactory.CreateInfo("Loading plugin", category: "PluginService", data: new Dictionary<string, string>
        {
            { "PluginId", id },
        }));

        try
        {
            if (GetPluginInfoById(id) is { } existing)
            {
                // Already enabled: re-enabling is what toggling the state back on means.
                return existing.IsEnabled
                    ? await EnablePluginAsync(id, suppressNotification).ConfigureAwait(false)
                    : await DescribeDisabledPluginAsync(id, existing, suppressNotification).ConfigureAwait(false);
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
        await Task.Run(CleanupStalePluginTempDirectories).ConfigureAwait(false);

        try
        {
            if (!Directory.Exists(PluginsDirectory))
            {
                return;
            }

            string[] enabledPluginFiles = Directory.GetFiles(PluginsDirectory, $"*{EnabledExtension}");
            string[] disabledPluginFiles = Directory.GetFiles(PluginsDirectory, $"*{DisabledExtension}");

            foreach (string pluginFile in enabledPluginFiles.Concat(disabledPluginFiles))
            {
                string pluginId = Path.GetFileNameWithoutExtension(pluginFile);
                await LoadPluginAsync(pluginId, suppressNotification: true).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enumerate plugins in '{Directory}'", PluginsDirectory);
            SentrySdk.CaptureException(ex);
        }

        // Always notify: a page opened while this scan was still running has to be refreshed even when
        // every plugin failed to load, otherwise it keeps showing the empty state forever.
        RaisePluginsChanged();
    }

    public void UnloadPlugin(string id)
    {
        UnloadLoadContext(id);

        string? pluginDirectory;
        lock (pluginStateLock)
        {
            plugins.RemoveAll(plugin => plugin.Manifest.Id == id);

            if (pluginInfos.TryGetValue(id, out PluginInfo? pluginInfo))
            {
                pluginInfo.Instance = null;
            }

            pluginPaths.TryGetValue(id, out pluginDirectory);
            RaisePluginsChanged();
        }

        if (pluginDirectory is not null)
        {
            DeletePluginTempDirectory(pluginDirectory);
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
            PluginInfo? pluginInfo = GetPluginInfoById(id);
            pluginInfo?.Instance?.OnUninstall();

            File.Delete(GetPluginFilePath(id, EnabledExtension));
            File.Delete(GetPluginFilePath(id, DisabledExtension));

            UnloadPlugin(id);

            lock (pluginStateLock)
            {
                pluginInfos.Remove(id);
                pluginPaths.Remove(id);
                RaisePluginsChanged();
            }

            messenger.Send(InfoBarMessage.Success(SH.FormatServicePluginDeleteSuccess(id)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to uninstall plugin '{PluginId}'", id);
            SentrySdk.CaptureException(ex);
            messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginDeleteFailed(id), ex));
        }
    }

    public string? GetPluginPath(string id)
    {
        lock (pluginStateLock)
        {
            if (pluginPaths.TryGetValue(id, out string? path))
            {
                return path;
            }
        }

        return null;
    }

    public string GetPluginsDirectory()
    {
        return PluginsDirectory;
    }

    public IPluginSettingService GetSettingService()
    {
        return serviceProvider.GetRequiredService<IPluginSettingService>();
    }

    /// <summary>
    /// Gets a weak reference to the load context of a loaded plugin, so a caller (or a test) can
    /// observe whether disabling the plugin actually released its assemblies.
    /// </summary>
    public bool TryGetPluginLoadContext(string id, [NotNullWhen(true)] out WeakReference? loadContextReference)
    {
        lock (pluginStateLock)
        {
            if (pluginLoadContexts.TryGetValue(id, out PluginAssemblyLoadContext? loadContext))
            {
                loadContextReference = new WeakReference(loadContext);
                return true;
            }
        }

        loadContextReference = null;
        return false;
    }

    /// <summary>
    /// Notifies listeners on the main thread. The service is a singleton while the listeners are scoped
    /// views, so raising on the calling (background) thread would have them touch UI-bound collections
    /// off the UI thread. <see cref="ITaskContext.BeginInvokeOnMainThread"/> requires a static delegate,
    /// hence the instance field.
    /// </summary>
    private void RaisePluginsChanged()
    {
        taskContext.BeginInvokeOnMainThread(NotifyPluginsChanged);
    }

    private void NotifyPluginsChanged()
    {
        PluginsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Builds the per-process, per-instance scratch root. Unpacked plugins are keyed by process id first
    /// so that stale directories of a crashed instance can be distinguished from a live parallel one.
    /// </summary>
    private static string CreateInstanceTempDirectory()
    {
        string parent = Path.Combine(Path.GetTempPath(), TempRootDirectoryName, TempPluginsDirectoryName);
        string directory = Path.Combine(parent, $"{Environment.ProcessId}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
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

    private static string LocateEntryAssembly(string pluginDirectory, string pluginId)
    {
        string dllPath = Path.Combine(pluginDirectory, $"{pluginId}.dll");
        if (File.Exists(dllPath))
        {
            return dllPath;
        }

        dllPath = Path.Combine(pluginDirectory, "lib", $"{pluginId}.dll");
        HutaoException.ThrowIfNot(File.Exists(dllPath), $"Plugin '{pluginId}' does not contain '{pluginId}.dll'.");
        return dllPath;
    }

    private async Task<bool> LoadPluginAsyncCore(string id, bool suppressNotification)
    {
        (bool isEnabled, string pluginFilePath) = LocatePluginFile(id);
        if (pluginFilePath.Length == 0)
        {
            messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginLoadDirectoryNotFound(id)));
            return false;
        }

        string pluginDirectory = CreatePluginTempDirectory(id);
        HutaoPlugin? plugin = null;
        bool succeeded = false;
        PluginAssemblyLoadContext? loadContext = null;

        // Everything the plugin ships stays in its own collectible context, so disabling the plugin
        // releases those assemblies instead of leaking them into the host for the process lifetime.
        try
        {
            using ZipArchive archive = ZipFile.OpenRead(pluginFilePath);

            PluginManifest? manifest = await ReadManifestAsync(archive).ConfigureAwait(false);
            if (manifest is null)
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginLoadInvalidManifest(id)));
                return false;
            }

            archive.ExtractToDirectory(pluginDirectory, true);

            if (isEnabled)
            {
                string dllPath = LocateEntryAssembly(pluginDirectory, id);
                string assemblyName = Path.GetFileNameWithoutExtension(dllPath);

                loadContext = new PluginAssemblyLoadContext(id, pluginDirectory);
                plugin = CreatePluginInstance(loadContext, assemblyName, manifest);
                plugin.Manifest = manifest;

                string pluginIconPath = Path.Combine(pluginDirectory, "icon.png");
                if (File.Exists(pluginIconPath))
                {
                    plugin.IconPath = pluginIconPath;
                }

                plugin.OnLoad(new PluginContext(serviceProvider));
            }

            PluginInfo pluginInfo = RegisterPluginInfo(id, manifest, pluginDirectory, plugin, isEnabled);

            if (isEnabled)
            {
                lock (pluginStateLock)
                {
                    plugins.Add(plugin!);
                    pluginLoadContexts[id] = loadContext!;
                }

                // The package was already found in its enabled location, so unlike EnablePluginAsync there
                // is no file to move and nothing that can fail; only the plugin's own hook is left to run.
                pluginInfo.Instance?.OnEnable();
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
            // Every path that does not end with a usable, described plugin has to release both the
            // context and the unpacked directory, otherwise they survive for the process lifetime.
            if (!succeeded)
            {
                if (plugin is not null)
                {
                    lock (pluginStateLock)
                    {
                        plugins.Remove(plugin);
                        pluginInfos.Remove(id);
                    }
                }

                UnloadLoadContext(id);
                DeletePluginTempDirectory(pluginDirectory);
            }
        }
    }

    /// <summary>
    /// Refreshes a tracked disabled plugin from its package. The assemblies stay unloaded; only the
    /// description is read, which is what the plugin manager lists.
    /// </summary>
    private async Task<bool> DescribeDisabledPluginAsync(string id, PluginInfo pluginInfo, bool suppressNotification)
    {
        string pluginFilePath = GetPluginFilePath(id, DisabledExtension);
        if (!File.Exists(pluginFilePath))
        {
            messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginLoadDirectoryNotFound(id)));
            return false;
        }

        string pluginDirectory = CreatePluginTempDirectory(id);
        bool succeeded = false;

        try
        {
            using ZipArchive archive = ZipFile.OpenRead(pluginFilePath);

            PluginManifest? manifest = await ReadManifestAsync(archive).ConfigureAwait(false);
            if (manifest is null)
            {
                messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginLoadInvalidManifest(id)));
                return false;
            }

            archive.ExtractToDirectory(pluginDirectory, true);
            RegisterPluginInfo(id, manifest, pluginDirectory, instance: null, isEnabled: false);

            succeeded = true;
            return true;
        }
        finally
        {
            if (!succeeded)
            {
                DeletePluginTempDirectory(pluginDirectory);
            }
        }
    }

    private (bool IsEnabled, string PluginFilePath) LocatePluginFile(string id)
    {
        string enabledPath = GetPluginFilePath(id, EnabledExtension);
        if (File.Exists(enabledPath))
        {
            return (true, enabledPath);
        }

        string disabledPath = GetPluginFilePath(id, DisabledExtension);
        return File.Exists(disabledPath) ? (false, disabledPath) : (false, string.Empty);
    }

    /// <summary>
    /// Creates the description on first load and refreshes its content when the package was replaced,
    /// without discarding the live instance of an enabled plugin.
    /// </summary>
    private PluginInfo RegisterPluginInfo(string id, PluginManifest manifest, string pluginDirectory, HutaoPlugin? instance, bool isEnabled)
    {
        string iconPath = Path.Combine(pluginDirectory, "icon.png");
        if (!File.Exists(iconPath))
        {
            iconPath = string.Empty;
        }

        lock (pluginStateLock)
        {
            if (pluginInfos.TryGetValue(id, out PluginInfo? existing))
            {
                existing.Manifest = manifest;
                existing.IconPath = iconPath;
                existing.Instance = instance ?? existing.Instance;
                RaisePluginsChanged();
                return existing;
            }

            PluginInfo pluginInfo = new()
            {
                Id = id,
                Manifest = manifest,
                IconPath = iconPath,
                IsEnabled = isEnabled,
                Instance = instance,
            };

            pluginInfos[id] = pluginInfo;
            pluginPaths[id] = pluginDirectory;
            RaisePluginsChanged();
            return pluginInfo;
        }
    }

    /// <summary>
    /// Loads the assembly of a described but currently unloaded plugin, so it can be enabled.
    /// </summary>
    private async Task<bool> LoadPluginInstanceAsync(PluginInfo pluginInfo)
    {
        string id = pluginInfo.Id;
        (_, string pluginFilePath) = LocatePluginFile(id);
        if (pluginFilePath.Length == 0)
        {
            messenger.Send(InfoBarMessage.Error(SH.FormatServicePluginLoadDirectoryNotFound(id)));
            return false;
        }

        string pluginDirectory = CreatePluginTempDirectory(id);

        try
        {
            using ZipArchive archive = ZipFile.OpenRead(pluginFilePath);
            archive.ExtractToDirectory(pluginDirectory, true);
        }
        catch (Exception ex)
        {
            DeletePluginTempDirectory(pluginDirectory);
            logger.LogError(ex, "Failed to unpack plugin '{PluginId}'", id);
            SentrySdk.CaptureException(ex);
            messenger.Send(InfoBarMessage.Error(SH.ServicePluginLoadFailed, ex));
            return false;
        }

        try
        {
            string dllPath = LocateEntryAssembly(pluginDirectory, id);
            PluginAssemblyLoadContext loadContext = new(id, pluginDirectory);
            HutaoPlugin plugin = CreatePluginInstance(loadContext, Path.GetFileNameWithoutExtension(dllPath), pluginInfo.Manifest);
            plugin.Manifest = pluginInfo.Manifest;

            string iconPath = Path.Combine(pluginDirectory, "icon.png");
            if (File.Exists(iconPath))
            {
                plugin.IconPath = iconPath;
            }

            plugin.OnLoad(new PluginContext(serviceProvider));

            lock (pluginStateLock)
            {
                plugins.Add(plugin);
                pluginLoadContexts[id] = loadContext;
                pluginPaths[id] = pluginDirectory;

                pluginInfo.Instance = plugin;
                pluginInfo.IconPath = plugin.IconPath;
            }

            return true;
        }
        catch (Exception ex)
        {
            UnloadLoadContext(id);
            DeletePluginTempDirectory(pluginDirectory);
            logger.LogError(ex, "Failed to load plugin '{PluginId}'", id);
            SentrySdk.CaptureException(ex);
            messenger.Send(InfoBarMessage.Error(SH.ServicePluginLoadFailed, ex));
            return false;
        }
    }

    private HutaoPlugin CreatePluginInstance(PluginAssemblyLoadContext loadContext, string assemblyName, PluginManifest manifest)
    {
        Assembly assembly;
        Type? pluginType;

        try
        {
            assembly = loadContext.LoadPluginAssembly(assemblyName);
            pluginType = FindPluginMainType(assembly);
        }
        catch (Exception ex) when (ex is not HutaoException)
        {
            throw HutaoException.Throw($"Plugin '{manifest.Id}' assembly could not be loaded or inspected.", ex);
        }

        if (pluginType is null)
        {
            throw HutaoException.Throw(SH.FormatServicePluginLoadMainTypeNotFound(manifest.Name));
        }

        // The entry point is declared in code by [PluginMain], so a missing parameterless constructor
        // is a plugin authoring error and must not surface as a bare MissingMethodException.
        ConstructorInfo? constructor = pluginType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            throw HutaoException.Throw($"Plugin '{manifest.Id}' entry type '{pluginType.FullName}' must declare a public parameterless constructor.");
        }

        try
        {
            return (HutaoPlugin)constructor.Invoke(null);
        }
        catch (TargetInvocationException ex)
        {
            throw HutaoException.Throw($"Plugin '{manifest.Id}' constructor threw an exception.", ex.InnerException ?? ex);
        }
        catch (Exception ex) when (ex is not HutaoException)
        {
            throw HutaoException.Throw($"Plugin '{manifest.Id}' entry type '{pluginType.FullName}' could not be instantiated.", ex);
        }
    }

    private void UnloadLoadContext(string id)
    {
        PluginAssemblyLoadContext? loadContext;
        lock (pluginStateLock)
        {
            if (!pluginLoadContexts.Remove(id, out loadContext))
            {
                return;
            }
        }

        loadContext.UnloadAndDetach();
        logger.LogInformation("Plugin '{PluginId}' load context unloaded", id);
    }

    private string GetPluginFilePath(string pluginId, string extension)
    {
        return Path.Combine(PluginsDirectory, $"{pluginId}{extension}");
    }

    private string CreatePluginTempDirectory(string pluginId)
    {
        string pluginDirectory = Path.Combine(instanceTempDirectory, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        return pluginDirectory;
    }

    private static void DeletePluginTempDirectory(string pluginDirectory)
    {
        try
        {
            if (Directory.Exists(pluginDirectory))
            {
                Directory.Delete(pluginDirectory, true);
            }
        }
        catch (IOException)
        {
            // A plugin page may still hold a file handle; the next start reclaims the directory.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Reclaims unpacked plugin directories that cannot belong to a live process. Directories owned by
    /// another running instance are deliberately left alone, so parallel instances never delete each
    /// other's plugins.
    /// </summary>
    private void CleanupStalePluginTempDirectories()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), TempRootDirectoryName, TempPluginsDirectoryName);
        CleanupStaleLegacyTempDirectories();

        if (!Directory.Exists(baseDirectory))
        {
            return;
        }

        foreach (DirectoryInfo instanceDirectory in new DirectoryInfo(baseDirectory).GetDirectories())
        {
            if (IsOwnInstanceTempDirectory(instanceDirectory.Name))
            {
                continue;
            }

            if (!TryGetOwnerProcessId(instanceDirectory.Name, out int processId) || IsProcessAlive(processId))
            {
                continue;
            }

            try
            {
                instanceDirectory.Delete(true);
            }
            catch (IOException ex)
            {
                logger.LogDebug(ex, "Failed to reclaim stale plugin directory '{Directory}'", instanceDirectory.FullName);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogDebug(ex, "Failed to reclaim stale plugin directory '{Directory}'", instanceDirectory.FullName);
            }
        }
    }

    /// <summary>
    /// Removes directories left behind by the pre-isolation layout. That layout named a directory after
    /// the plugin id alone, which cannot be distinguished from another instance's, so the sweep runs once
    /// at startup before this instance unpacks anything of its own.
    /// </summary>
    private void CleanupStaleLegacyTempDirectories()
    {
        DirectoryInfo tempRoot = new(Path.GetTempPath());

        foreach (DirectoryInfo legacyDirectory in tempRoot.GetDirectories($"{LegacyTempDirectoryPrefix}*"))
        {
            try
            {
                legacyDirectory.Delete(true);
            }
            catch (IOException)
            {
                // In use by another instance; it will be reclaimed on a later start.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private bool IsOwnInstanceTempDirectory(string name)
    {
        return string.Equals(name, Path.GetFileName(instanceTempDirectory), StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetOwnerProcessId(string directoryName, out int processId)
    {
        processId = 0;
        int separatorIndex = directoryName.IndexOf('_', StringComparison.Ordinal);
        return separatorIndex > 0 && int.TryParse(directoryName.AsSpan(0, separatorIndex), out processId);
    }

    private static bool IsProcessAlive(int processId)
    {
        try
        {
            using System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private Type? FindPluginMainType(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<PluginMainAttribute>() != null &&
                typeof(HutaoPlugin).IsAssignableFrom(type))
            {
                return type;
            }
        }

        return null;
    }
}
