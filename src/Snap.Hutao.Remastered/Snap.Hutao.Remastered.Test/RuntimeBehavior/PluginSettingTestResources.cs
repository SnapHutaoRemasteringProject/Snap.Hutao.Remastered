using Microsoft.Extensions.Logging;

namespace Snap.Hutao.Remastered.Service.Plugin;

// The portable tests link the production settings service, with only its
// generated constructor and the plugin directory dependency replaced.
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ServiceAttribute : Attribute
{
    public ServiceAttribute(ServiceLifetime lifetime, Type contract) { }
}

[AttributeUsage(AttributeTargets.Constructor)]
internal sealed class GeneratedConstructorAttribute : Attribute;

public interface IPluginService
{
    string GetPluginsDirectory();
}

public partial class PluginSettingService
{
    public partial PluginSettingService(IServiceProvider serviceProvider)
    {
        logger = serviceProvider.GetRequiredService<ILogger<PluginSettingService>>();
        pluginService = serviceProvider.GetRequiredService<IPluginService>();
    }
}
