using Snap.Hutao.Remastered.API.Model.Plugin;
using System.Collections.Frozen;
using System.IO;

namespace Snap.Hutao.Remastered.Service.Plugin;

[Service(ServiceLifetime.Singleton, typeof(IPluginSettingService))]
public partial class PluginSettingService : IPluginSettingService
{
    private static readonly FrozenSet<Type> SupportedTypes =
    [
        typeof(int),
        typeof(bool),
        typeof(string),
    ];
    private readonly Dictionary<string, Dictionary<string, object>> settingsCache = new();
    private readonly Dictionary<string, List<RegisteredSettingInfo>> registeredSettings = new();
    private readonly object lockObject = new();

    private readonly IPluginService pluginService;

    [GeneratedConstructor]
    public partial PluginSettingService(IServiceProvider serviceProvider);


    public async Task<T?> GetSettingAsync<T>(string pluginId, string settingName, T? defaultValue = default)
    {
        if (!IsSupportedType<T>())
        {
            throw new NotSupportedException($"Type '{typeof(T)}' is not supported for plugin settings.");
        }

        try
        {
            lock (lockObject)
            {
                if (settingsCache.TryGetValue(pluginId, out Dictionary<string, object>? pluginSettings) &&
                    pluginSettings.TryGetValue(settingName, out object? cachedValue))
                {
                    return (T?)cachedValue;
                }
            }

            string filePath = GetSettingFilePath(pluginId, settingName);
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                T? value = JsonSerializer.Deserialize<T>(json, options);

                lock (lockObject)
                {
                    if (!settingsCache.ContainsKey(pluginId))
                    {
                        settingsCache[pluginId] = new Dictionary<string, object>();
                    }
                    if (value != null)
                    {
                        settingsCache[pluginId][settingName] = value;
                    }
                }

                return value;
            }

            lock (lockObject)
            {
                if (!settingsCache.ContainsKey(pluginId))
                {
                    settingsCache[pluginId] = new Dictionary<string, object>();
                }
                if (defaultValue != null)
                {
                    settingsCache[pluginId][settingName] = defaultValue;
                }
            }

            return defaultValue;
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    public bool TryGetCachedSetting<T>(string pluginId, string settingName, out T? value)
    {
        lock (lockObject)
        {
            if (settingsCache.TryGetValue(pluginId, out Dictionary<string, object>? pluginSettings) &&
                pluginSettings.TryGetValue(settingName, out object? cachedValue) &&
                cachedValue is T)
            {
                value = (T)cachedValue;
                return true;
            }
        }

        value = default;
        return false;
    }

    public async Task<object?> GetSettingAsync(Type type, string pluginId, string settingName, object? defaultValue = default)
    {
        if (!IsSupportedType(type))
        {
            throw new NotSupportedException($"Type '{type}' is not supported for plugin settings.");
        }

        try
        {
            lock (lockObject)
            {
                if (settingsCache.TryGetValue(pluginId, out Dictionary<string, object>? pluginSettings) &&
                    pluginSettings.TryGetValue(settingName, out object? cachedValue))
                {
                    return cachedValue;
                }
            }

            string filePath = GetSettingFilePath(pluginId, settingName);
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                object? value = JsonSerializer.Deserialize(json, type, options);

                lock (lockObject)
                {
                    if (!settingsCache.ContainsKey(pluginId))
                    {
                        settingsCache[pluginId] = new Dictionary<string, object>();
                    }
                    if (value != null)
                    {
                        settingsCache[pluginId][settingName] = value;
                    }
                }

                return value;
            }

            lock (lockObject)
            {
                if (!settingsCache.ContainsKey(pluginId))
                {
                    settingsCache[pluginId] = new Dictionary<string, object>();
                }
                if (defaultValue != null)
                {
                    settingsCache[pluginId][settingName] = defaultValue;
                }
            }

            return defaultValue;
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    public async Task SetSettingAsync<T>(string pluginId, string settingName, T? value)
    {
        if (!IsSupportedType<T>())
        {
            throw new NotSupportedException($"Type '{typeof(T)}' is not supported for plugin settings.");
        }

        try
        {
            lock (lockObject)
            {
                if (!settingsCache.ContainsKey(pluginId))
                {
                    settingsCache[pluginId] = new Dictionary<string, object>();
                }
                settingsCache[pluginId][settingName] = value!;
            }

            string pluginFolder = Path.Combine(pluginService.GetPluginsDirectory(), pluginId);
            Directory.CreateDirectory(pluginFolder);

            string filePath = Path.Combine(pluginFolder, $"{settingName}.json");

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string json = value != null ? JsonSerializer.Serialize(value, options) : string.Empty;
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save setting '{settingName}' for plugin '{pluginId}'", ex);
        }
    }

    public async Task SetSettingAsync(Type type, string pluginId, string settingName, object? value)
    {
        if (!IsSupportedType(type))
        {
            throw new NotSupportedException($"Type '{type}' is not supported for plugin settings.");
        }

        try
        {
            lock (lockObject)
            {
                if (!settingsCache.ContainsKey(pluginId))
                {
                    settingsCache[pluginId] = new Dictionary<string, object>();
                }
                settingsCache[pluginId][settingName] = value!;
            }

            string pluginFolder = Path.Combine(pluginService.GetPluginsDirectory(), pluginId);
            Directory.CreateDirectory(pluginFolder);

            string filePath = Path.Combine(pluginFolder, $"{settingName}.json");

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string json = value != null ? JsonSerializer.Serialize(value, options) : string.Empty;
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save setting '{settingName}' for plugin '{pluginId}'", ex);
        }
    }

    public void RegisterSetting<T>(string pluginId, string settingName, T? defaultValue = default, string? description = null)
    {
        if (!IsSupportedType<T>())
        {
            throw new NotSupportedException($"Type '{typeof(T)}' is not supported for plugin settings.");
        }

        // Read the persisted value (if any) outside the lock to avoid holding it during file I/O.
        object? persistedValue = LoadPersistedSetting<T>(pluginId, settingName);

        lock (lockObject)
        {
            if (IsSettingRegistered(pluginId, settingName))
            {
                throw new InvalidOperationException($"Setting '{settingName}' for plugin '{pluginId}' is already registered.");
            }

            if (!registeredSettings.TryGetValue(pluginId, out List<RegisteredSettingInfo>? list))
            {
                registeredSettings[pluginId] = list = [];
            }

            list.Add(new RegisteredSettingInfo(
                pluginId,
                settingName,
                typeof(T),
                defaultValue,
                description
            ));

            if (!settingsCache.TryGetValue(pluginId, out Dictionary<string, object>? cache))
            {
                settingsCache[pluginId] = cache = [];
            }

            // A persisted value wins over the default; only seed when non-null to keep prior behavior.
            if (persistedValue is not null)
            {
                cache[settingName] = persistedValue;
            }
            else if (defaultValue is not null)
            {
                cache[settingName] = defaultValue;
            }
        }
    }

    private object? LoadPersistedSetting<T>(string pluginId, string settingName)
    {
        string filePath = GetSettingFilePath(pluginId, settingName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
            };

            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public IReadOnlyDictionary<string, List<RegisteredSettingInfo>> GetAllRegisteredSettings()
    {
        lock (lockObject)
        {
            return registeredSettings.ToDictionary(
                kvp => kvp.Key,
                kvp => new List<RegisteredSettingInfo>(kvp.Value)
            );
        }
    }

    public IReadOnlyList<RegisteredSettingInfo> GetRegisteredSettings(string pluginId)
    {
        lock (lockObject)
        {
            if (registeredSettings.TryGetValue(pluginId, out List<RegisteredSettingInfo>? settings))
            {
                return new List<RegisteredSettingInfo>(settings);
            }
            return new List<RegisteredSettingInfo>();
        }
    }

    public bool IsSettingRegistered(string pluginId, string settingName)
    {
        lock (lockObject)
        {
            if (registeredSettings.TryGetValue(pluginId, out List<RegisteredSettingInfo>? settings))
            {
                return settings.Any(s => s.Name == settingName);
            }
            return false;
        }
    }

    public bool IsSupportedType(Type type)
    {
        return SupportedTypes.Contains(type);
    }

    public bool IsSupportedType<T>()
    {
        return SupportedTypes.Contains(typeof(T));
    }

    private string GetSettingFilePath(string pluginId, string settingName)
    {
        return Path.Combine(pluginService.GetPluginsDirectory(), pluginId, $"{settingName}.json");
    }
}
