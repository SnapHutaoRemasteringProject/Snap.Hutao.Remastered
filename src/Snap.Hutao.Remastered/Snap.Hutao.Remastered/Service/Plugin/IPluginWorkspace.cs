namespace Snap.Hutao.Remastered.Service.Plugin;

internal interface IPluginWorkspace
{
    string CreateDirectory(string pluginId);

    void DeleteDirectory(string pluginDirectory);

    Task CleanupStaleDirectoriesAsync();
}
