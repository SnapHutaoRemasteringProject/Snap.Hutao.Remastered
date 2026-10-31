using System.IO;
using System.Reflection;
using Snap.Hutao.Remastered.API.Annotation;
using Snap.Hutao.Remastered.API.Model;
using Snap.Hutao.Remastered.Core.ExceptionService;

namespace Snap.Hutao.Remastered.Service.Plugin;

/// <summary>
/// Turns an unpacked plugin directory into a live <see cref="HutaoPlugin"/> and releases it again. Every plugin
/// gets its own collectible <see cref="PluginAssemblyLoadContext"/>, which makes this the only type allowed to
/// load plugin assemblies. The caller owns the returned context and has to hand it back through
/// <see cref="Unload"/> when the plugin goes away or the load is abandoned.
/// </summary>
[Service(ServiceLifetime.Singleton, typeof(IPluginAssemblyLoader))]
internal sealed partial class PluginAssemblyLoader : IPluginAssemblyLoader
{
    private readonly ILogger<PluginAssemblyLoader> logger;

    [GeneratedConstructor]
    public partial PluginAssemblyLoader(IServiceProvider serviceProvider);

    public (HutaoPlugin Plugin, PluginAssemblyLoadContext LoadContext) Load(string pluginId, string pluginDirectory, PluginManifest manifest)
    {
        string entryAssemblyPath = LocateEntryAssembly(pluginDirectory, pluginId);
        PluginAssemblyLoadContext loadContext = new(pluginId, pluginDirectory);

        try
        {
            HutaoPlugin plugin = CreateInstance(loadContext, Path.GetFileNameWithoutExtension(entryAssemblyPath), manifest);
            plugin.Manifest = manifest;
            return (plugin, loadContext);
        }
        catch
        {
            // The context exists before the plugin object does: a package that cannot produce one must not
            // leave a collectible context behind that nobody will ever unload.
            loadContext.UnloadAndDetach();
            throw;
        }
    }

    public void Unload(string pluginId, PluginAssemblyLoadContext loadContext)
    {
        loadContext.UnloadAndDetach();
        logger.LogInformation("Plugin '{PluginId}' load context unloaded", pluginId);
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

    private static HutaoPlugin CreateInstance(PluginAssemblyLoadContext loadContext, string assemblyName, PluginManifest manifest)
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

        // The entry point is declared in code by [PluginMain], so a missing parameterless constructor is a
        // plugin authoring error and must not surface as a bare MissingMethodException.
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

    private static Type? FindPluginMainType(Assembly assembly)
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
