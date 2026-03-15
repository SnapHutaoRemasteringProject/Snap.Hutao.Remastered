namespace Snap.Hutao.Remastered.API.Model.Plugin;

public class RegisteredSettingInfo
{
    public string PluginId { get; }
    public string Name { get; }
    public string? Description { get; }
    public Type ValueType { get; }
    public object? DefaultValue { get; }
    
    public RegisteredSettingInfo(string pluginId, string name, Type valueType, object? defaultValue, string? description = null)
    {
        PluginId = pluginId;
        Name = name;
        ValueType = valueType;
        DefaultValue = defaultValue;
        Description = description;
    }
}
