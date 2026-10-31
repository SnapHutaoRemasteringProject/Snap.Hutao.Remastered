using System.IO;

namespace Snap.Hutao.Remastered.Service.Plugin;

/// <summary>
/// The scratch directories plugins are unpacked into. Every process owns a root named with its process id
/// first, so a tree left behind by a crashed instance can be told apart from one a live parallel instance is
/// still using, and a plugin's directory is derived from its id so describing it twice reuses the same path.
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IPluginWorkspace))]
internal sealed partial class PluginWorkspace : IPluginWorkspace
{
    // Layout used before unpacking became instance scoped; kept only so a stale tree can still be reclaimed.
    private const string LegacyTempDirectoryPrefix = "hutao_plugin_";

    private const string TempRootDirectoryName = "SnapHutaoRemastered";
    private const string TempPluginsDirectoryName = "plugins";

    private readonly ILogger<PluginWorkspace> logger;

    private readonly string instanceDirectory = CreateInstanceDirectory();

    [GeneratedConstructor]
    public partial PluginWorkspace(IServiceProvider serviceProvider);

    public string CreateDirectory(string pluginId)
    {
        string pluginDirectory = Path.Combine(instanceDirectory, pluginId);
        Directory.CreateDirectory(pluginDirectory);
        return pluginDirectory;
    }

    public void DeleteDirectory(string pluginDirectory)
    {
        try
        {
            if (Directory.Exists(pluginDirectory))
            {
                Directory.Delete(pluginDirectory, recursive: true);
            }
        }
        catch (IOException ex)
        {
            // A plugin page may still hold a file handle; the next start reclaims the directory.
            logger.LogDebug(ex, "Failed to delete plugin directory '{Directory}'", pluginDirectory);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogDebug(ex, "Failed to delete plugin directory '{Directory}'", pluginDirectory);
        }
    }

    public Task CleanupStaleDirectoriesAsync()
    {
        return Task.Run(CleanupStaleDirectories);
    }

    private void CleanupStaleDirectories()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), TempRootDirectoryName, TempPluginsDirectoryName);
        CleanupStaleLegacyDirectories();

        if (!Directory.Exists(baseDirectory))
        {
            return;
        }

        foreach (DirectoryInfo instanceDirectoryInfo in new DirectoryInfo(baseDirectory).GetDirectories())
        {
            if (IsOwnInstanceDirectory(instanceDirectoryInfo.Name))
            {
                continue;
            }

            // Directories owned by another running instance are left alone, so parallel instances never delete
            // each other's plugins.
            if (!TryGetOwnerProcessId(instanceDirectoryInfo.Name, out int processId) || IsProcessAlive(processId))
            {
                continue;
            }

            try
            {
                instanceDirectoryInfo.Delete(true);
            }
            catch (IOException ex)
            {
                logger.LogDebug(ex, "Failed to reclaim stale plugin directory '{Directory}'", instanceDirectoryInfo.FullName);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogDebug(ex, "Failed to reclaim stale plugin directory '{Directory}'", instanceDirectoryInfo.FullName);
            }
        }
    }

    /// <summary>
    /// Removes directories left behind by the pre-isolation layout, which named a directory after the plugin id
    /// alone and therefore cannot be distinguished from another instance's.
    /// </summary>
    private void CleanupStaleLegacyDirectories()
    {
        DirectoryInfo tempRoot = new(Path.GetTempPath());

        foreach (DirectoryInfo legacyDirectory in tempRoot.GetDirectories($"{LegacyTempDirectoryPrefix}*"))
        {
            try
            {
                legacyDirectory.Delete(true);
            }
            catch (IOException ex)
            {
                // In use by another instance; it will be reclaimed on a later start.
                logger.LogDebug(ex, "Failed to reclaim legacy plugin directory '{Directory}'", legacyDirectory.FullName);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogDebug(ex, "Failed to reclaim legacy plugin directory '{Directory}'", legacyDirectory.FullName);
            }
        }
    }

    private static string CreateInstanceDirectory()
    {
        string parent = Path.Combine(Path.GetTempPath(), TempRootDirectoryName, TempPluginsDirectoryName);
        string directory = Path.Combine(parent, $"{Environment.ProcessId}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private bool IsOwnInstanceDirectory(string name)
    {
        return string.Equals(name, Path.GetFileName(instanceDirectory), StringComparison.OrdinalIgnoreCase);
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
}
