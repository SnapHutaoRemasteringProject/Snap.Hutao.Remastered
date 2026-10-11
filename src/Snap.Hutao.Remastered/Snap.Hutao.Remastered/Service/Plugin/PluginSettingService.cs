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

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly Dictionary<string, Dictionary<string, object>> settingsCache = new();
    private readonly Dictionary<string, List<RegisteredSettingInfo>> registeredSettings = new();
    private readonly object lockObject = new();

    private readonly ILogger<PluginSettingService> logger;
    private readonly IPluginService pluginService;

    [GeneratedConstructor]
    public partial PluginSettingService(IServiceProvider serviceProvider);

    public async Task<T?> GetSettingAsync<T>(string pluginId, string settingName, T? defaultValue = default)
    {
        if (!IsSupportedType<T>())
        {
            throw new NotSupportedException($"Type '{typeof(T)}' is not supported for plugin settings.");
        }

        lock (lockObject)
        {
            if (settingsCache.TryGetValue(pluginId, out Dictionary<string, object>? pluginSettings) &&
                pluginSettings.TryGetValue(settingName, out object? cachedValue))
            {
                return (T?)cachedValue;
            }
        }

        string filePath = GetSettingFilePath(pluginId, settingName);
        T? value = await ReadSettingAsync<T>(pluginId, settingName, filePath, defaultValue).ConfigureAwait(false);
        SeedCache(pluginId, settingName, value);
        return value;
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

        lock (lockObject)
        {
            if (settingsCache.TryGetValue(pluginId, out Dictionary<string, object>? pluginSettings) &&
                pluginSettings.TryGetValue(settingName, out object? cachedValue))
            {
                return cachedValue;
            }
        }

        string filePath = GetSettingFilePath(pluginId, settingName);
        object? value = await ReadSettingAsync(pluginId, settingName, filePath, type, defaultValue).ConfigureAwait(false);
        SeedCache(pluginId, settingName, value);
        return value;
    }

    public async Task SetSettingAsync<T>(string pluginId, string settingName, T? value)
    {
        if (!IsSupportedType<T>())
        {
            throw new NotSupportedException($"Type '{typeof(T)}' is not supported for plugin settings.");
        }

        await WriteSettingAsync(pluginId, settingName, value).ConfigureAwait(false);
    }

    public async Task SetSettingAsync(Type type, string pluginId, string settingName, object? value)
    {
        if (!IsSupportedType(type))
        {
            throw new NotSupportedException($"Type '{type}' is not supported for plugin settings.");
        }

        await WriteSettingAsync(pluginId, settingName, value).ConfigureAwait(false);
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
            if (TryGetRegisteredSetting(pluginId, settingName, out RegisteredSettingInfo? existing))
            {
                // A disabled plugin is unloaded and re-registers its settings when it is loaded again;
                // only a genuine redefinition is an error.
                if (existing.ValueType != typeof(T))
                {
                    throw new InvalidOperationException($"Setting '{settingName}' for plugin '{pluginId}' is already registered with type '{existing.ValueType}'.");
                }

                logger.LogDebug("Setting '{SettingName}' for plugin '{PluginId}' was registered again after a reload", settingName, pluginId);
                return;
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

            // A persisted value wins over the default; only seed when non-null to keep prior behavior.
            if (persistedValue is not null)
            {
                SeedCacheLocked(pluginId, settingName, persistedValue);
            }
            else if (defaultValue is not null)
            {
                SeedCacheLocked(pluginId, settingName, defaultValue);
            }
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

            return [];
        }
    }

    public bool IsSettingRegistered(string pluginId, string settingName)
    {
        lock (lockObject)
        {
            return TryGetRegisteredSetting(pluginId, settingName, out _);
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

    private bool TryGetRegisteredSetting(string pluginId, string settingName, [NotNullWhen(true)] out RegisteredSettingInfo? setting)
    {
        if (registeredSettings.TryGetValue(pluginId, out List<RegisteredSettingInfo>? settings))
        {
            setting = settings.FirstOrDefault(s => s.Name == settingName);
            return setting is not null;
        }

        setting = null;
        return false;
    }

    private async ValueTask<T?> ReadSettingAsync<T>(string pluginId, string settingName, string filePath, T? defaultValue)
    {
        if (!File.Exists(filePath))
        {
            return defaultValue;
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, ReadOptions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            // A setting that cannot be read is reported instead of silently discarded; the caller still
            // gets the default so that a single corrupt file cannot take the whole plugin down.
            ReportSettingFailure(ex, pluginId, settingName, filePath);
            return defaultValue;
        }
    }

    private async ValueTask<object?> ReadSettingAsync(string pluginId, string settingName, string filePath, Type type, object? defaultValue)
    {
        if (!File.Exists(filePath))
        {
            return defaultValue;
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize(json, type, ReadOptions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            ReportSettingFailure(ex, pluginId, settingName, filePath);
            return defaultValue;
        }
    }

    private async ValueTask WriteSettingAsync(string pluginId, string settingName, object? value)
    {
        string pluginFolder = Path.Combine(pluginService.GetPluginsDirectory(), pluginId);
        string filePath = Path.Combine(pluginFolder, $"{settingName}.json");

        try
        {
            Directory.CreateDirectory(pluginFolder);
            string json = value is null ? string.Empty : JsonSerializer.Serialize(value, WriteOptions);

            // The file is the source of truth: only a durable write may update the cache, otherwise the
            // in-memory value would report success for a change that is lost on restart.
            await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save setting '{SettingName}' for plugin '{PluginId}'", settingName, pluginId);
            throw new InvalidOperationException($"Failed to save setting '{settingName}' for plugin '{pluginId}'", ex);
        }

        SeedCache(pluginId, settingName, value);
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
            return JsonSerializer.Deserialize<T>(File.ReadAllText(filePath), ReadOptions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            ReportSettingFailure(ex, pluginId, settingName, filePath);
            return null;
        }
    }

    private void SeedCache(string pluginId, string settingName, object? value)
    {
        if (value is null)
        {
            return;
        }

        lock (lockObject)
        {
            SeedCacheLocked(pluginId, settingName, value);
        }
    }

    private void SeedCacheLocked(string pluginId, string settingName, object value)
    {
        if (!settingsCache.TryGetValue(pluginId, out Dictionary<string, object>? cache))
        {
            settingsCache[pluginId] = cache = [];
        }

        cache[settingName] = value;
    }

    private void ReportSettingFailure(Exception ex, string pluginId, string settingName, string filePath)
    {
        logger.LogError(ex, "Failed to read setting '{SettingName}' for plugin '{PluginId}' from '{FilePath}'", settingName, pluginId, filePath);
        SentrySdk.CaptureException(ex);
    }

    private string GetSettingFilePath(string pluginId, string settingName)
    {
        return Path.Combine(pluginService.GetPluginsDirectory(), pluginId, $"{settingName}.json");
    }
}
