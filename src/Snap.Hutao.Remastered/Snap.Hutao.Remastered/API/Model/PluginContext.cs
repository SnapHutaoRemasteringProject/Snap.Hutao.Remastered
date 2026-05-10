using Snap.Hutao.Remastered.Service.Plugin;

namespace Snap.Hutao.Remastered.API.Model;

public class PluginContext
{
    public IServiceProvider ServiceProvider { get; }
    public IPluginService PluginService { get; }

    public PluginContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        PluginService = serviceProvider.GetRequiredService<IPluginService>();
    }
}
