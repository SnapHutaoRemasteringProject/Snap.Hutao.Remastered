using Snap.Hutao.Remastered.Service.Plugin;

namespace Snap.Hutao.Remastered.API.Setting;

public class PluginSetting<T>
{
    private readonly IPluginSettingService pluginSettingService;
    private string? pluginId;
    private bool isRegistered = false;
    private T? value;

    public IPluginService PluginService { get; }
    public string Name { get; set; } = default!;
    public T? Value
    {
        get => GetValue();

        set => SetValue(value);
    }
    public T? DefaultValue { get; set; }
    public string? Description { get; set; }

    public PluginSetting(IPluginService pluginService, string name, T? defaultValue = default)
    {
        PluginService = pluginService;
        pluginSettingService = pluginService.GetSettingService();
        Name = name;
        DefaultValue = defaultValue;
        Value = defaultValue;
    }

    public void Register(string pluginId, string? description = null)
    {
        if (isRegistered)
        {
            throw new InvalidOperationException($"Setting '{Name}' is already registered.");
        }

        pluginSettingService.RegisterSetting(pluginId, Name, DefaultValue, description ?? Description);
        isRegistered = true;
        this.pluginId = pluginId;
    }

    public void SetValue(T? value)
    {
        this.value = value;
        
        if (isRegistered && pluginSettingService != null && pluginId != null)
        {
            _ = pluginSettingService.SetSettingAsync(pluginId, Name, value);
        }
    }

    public T? GetValue()
    {
        if (isRegistered && pluginSettingService != null && pluginId != null)
        {
            value = Task.Run(() => pluginSettingService.GetSettingAsync(pluginId, Name, DefaultValue)).Result;
        }

        return value;
    }

    public bool IsRegistered => isRegistered;
}
