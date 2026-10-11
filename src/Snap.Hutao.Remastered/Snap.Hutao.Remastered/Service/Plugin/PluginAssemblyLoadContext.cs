using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Snap.Hutao.Remastered.Service.Plugin;

/// <summary>
/// A collectible load context that gives every plugin its own assembly universe.
/// Assemblies shipped inside the plugin package are loaded into this context and are
/// therefore released when the context is unloaded; everything the plugin does not ship
/// is resolved from the host, so types crossing the plugin boundary (such as
/// <c>HutaoPlugin</c>) keep a single identity.
/// </summary>
public sealed class PluginAssemblyLoadContext : AssemblyLoadContext
{
    private const string LibDirectoryName = "lib";

    /// <summary>
    /// Name of the assembly that carries the plugin contract. It is always resolved from the host so a
    /// plugin package can never introduce a second copy of <c>HutaoPlugin</c>.
    /// </summary>
    private static readonly string ContractAssemblyName = typeof(API.Model.HutaoPlugin).Assembly.GetName().Name!;

    private readonly string pluginDirectory;
    private readonly Dictionary<string, string> localAssemblies;

    public PluginAssemblyLoadContext(string pluginId, string pluginDirectory)
        : base($"HutaoPlugin:{pluginId}", isCollectible: true)
    {
        this.pluginDirectory = pluginDirectory;
        localAssemblies = ScanLocalAssemblies(pluginDirectory);

        Resolving += OnResolving;
    }

    /// <summary>
    /// Loads an assembly by simple name from the plugin package. Exposed so that a failure always points
    /// at the plugin package rather than at some probing path.
    /// </summary>
    public Assembly LoadPluginAssembly(string simpleName)
    {
        return localAssemblies.TryGetValue(simpleName, out string? path)
            ? LoadFromAssemblyPath(path)
            : throw new FileNotFoundException($"Assembly '{simpleName}' was not found in plugin package '{pluginDirectory}'.");
    }

    /// <summary>
    /// Detaches the resolver before unloading. A context that still subscribes to <c>Resolving</c> stays
    /// reachable from the runtime, which would otherwise keep the plugin's assemblies alive.
    /// </summary>
    public void UnloadAndDetach()
    {
        Resolving -= OnResolving;
        Unload();
    }

    private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        string? simpleName = assemblyName.Name;
        if (simpleName is null)
        {
            return null;
        }

        // The host contract assembly must never be duplicated: HutaoPlugin and every type the plugin
        // hands back to the host live there, and a second copy would fail every type check.
        if (string.Equals(simpleName, ContractAssemblyName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Otherwise the copy the plugin shipped wins. Delegating to the host whenever the host happens to
        // have the assembly loaded already would make resolution depend on host startup order.
        if (localAssemblies.TryGetValue(simpleName, out string? path))
        {
            return LoadFromAssemblyPath(path);
        }

        // Nothing shipped locally: framework, host and already-loaded assemblies come from the host.
        return null;
    }

    private static Dictionary<string, string> ScanLocalAssemblies(string pluginDirectory)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);

        foreach (string directory in (string[])[pluginDirectory, Path.Combine(pluginDirectory, LibDirectoryName)])
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                string? name = TryReadAssemblyName(file);
                if (name is not null && !result.ContainsKey(name))
                {
                    result[name] = file;
                }
            }
        }

        return result;
    }

    private static string? TryReadAssemblyName(string file)
    {
        // Managed and native dlls are mixed in plugin packages; only the former can be loaded as assemblies.
        try
        {
            return AssemblyName.GetAssemblyName(file).Name;
        }
        catch (BadImageFormatException)
        {
            return null;
        }
        catch (FileLoadException)
        {
            return null;
        }
    }
}
