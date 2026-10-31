using Snap.Hutao.Remastered.API.Model;

namespace Snap.Hutao.Remastered.Service.Plugin;

internal interface IPluginRegistry
{
    event EventHandler? Changed;

    void RaiseChanged();

    PluginInfo? GetInfo(string id);

    IReadOnlyList<PluginInfo> GetAllInfos();

    IReadOnlyList<HutaoPlugin> GetAllPlugins();

    string? GetPath(string id);

    bool TryGetLoadContextReference(string id, [NotNullWhen(true)] out WeakReference? loadContextReference);

    ValueTask<PluginInfo> DescribeAsync(string id, PluginManifest manifest, string pluginDirectory, HutaoPlugin? instance, bool isEnabled);

    ValueTask SetEnabledAsync(PluginInfo pluginInfo, bool isEnabled);

    ValueTask SetIconPathAsync(PluginInfo pluginInfo, string iconPath);

    void Attach(string id, HutaoPlugin plugin, PluginAssemblyLoadContext loadContext, string pluginDirectory);

    PluginAssemblyLoadContext? TakeLoadContext(string id);

    string? Detach(string id);

    void Remove(string id);
}
