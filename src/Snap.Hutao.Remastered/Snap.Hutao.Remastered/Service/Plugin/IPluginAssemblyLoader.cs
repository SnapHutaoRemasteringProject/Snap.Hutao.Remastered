using Snap.Hutao.Remastered.API.Model;

namespace Snap.Hutao.Remastered.Service.Plugin;

internal interface IPluginAssemblyLoader
{
    (HutaoPlugin Plugin, PluginAssemblyLoadContext LoadContext) Load(string pluginId, string pluginDirectory, PluginManifest manifest);

    void Unload(string pluginId, PluginAssemblyLoadContext loadContext);
}
