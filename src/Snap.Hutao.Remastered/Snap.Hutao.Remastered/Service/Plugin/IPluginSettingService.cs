using Snap.Hutao.Remastered.API.Model.Plugin;

namespace Snap.Hutao.Remastered.Service.Plugin;

public interface IPluginSettingService
{
    Task<T?> GetSettingAsync<T>(string pluginId, string settingName, T? defaultValue = default);

    bool TryGetCachedSetting<T>(string pluginId, string settingName, out T? value);

    Task<object?> GetSettingAsync(Type type, string pluginId, string settingName, object? defaultValue = default);

    Task SetSettingAsync<T>(string pluginId, string settingName, T? value);

    Task SetSettingAsync(Type type, string pluginId, string settingName, object? value);

    void RegisterSetting<T>(string pluginId, string settingName, T? defaultValue = default, string? description = null);
    
    IReadOnlyDictionary<string, List<RegisteredSettingInfo>> GetAllRegisteredSettings();
    
    IReadOnlyList<RegisteredSettingInfo> GetRegisteredSettings(string pluginId);
    
    bool IsSettingRegistered(string pluginId, string settingName);

    bool IsSupportedType(Type type);

    bool IsSupportedType<T>();
}
