// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Factory.Process;
using Snap.Hutao.Remastered.Win32;
using Snap.Hutao.Remastered.Win32.Foundation;
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using Windows.Storage;

namespace Snap.Hutao.Remastered.Core.Setting;

public static class LocalSetting
{
    private static readonly FrozenSet<Type> SupportedTypes =
    [
        typeof(byte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(bool),
        typeof(char),
        typeof(string),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Windows.Foundation.Point),
        typeof(Windows.Foundation.Size),
        typeof(Windows.Foundation.Rect),
        typeof(ApplicationDataCompositeValue)
    ];

    private static readonly ApplicationDataContainer? PackagedContainer;
    private static readonly FileSettingsStore? UnpackagedStore;

    static LocalSetting()
    {
        if (RuntimeEnvironment.IsPackaged)
        {
            PackagedContainer = ApplicationData.Current.LocalSettings;
        }
        else
        {
            UnpackagedStore = new FileSettingsStore();
        }
    }

    public static T Get<T>(string key, T defaultValue = default!)
    {
        Debug.Assert(SupportedTypes.Contains(typeof(T)));

        if (RuntimeEnvironment.IsPackaged)
        {
            if (PackagedContainer!.Values.TryGetValue(key, out object? value))
            {
                return value is null ? defaultValue : (T)value;
            }

            Set(key, defaultValue);
            return defaultValue;
        }
        else
        {
            return UnpackagedStore!.Get(key, defaultValue);
        }
    }

    public static void Set<T>(string key, T value)
    {
        Debug.Assert(SupportedTypes.Contains(typeof(T)));

        if (RuntimeEnvironment.IsPackaged)
        {
            try
            {
                PackagedContainer!.Values[key] = value;
            }
            catch (Exception ex)
            {
                if (HutaoNative.IsWin32(ex.HResult, WIN32_ERROR.ERROR_STATE_WRITE_SETTING_FAILED))
                {
                    HutaoNative.Instance.ShowErrorMessage(ex.Message, ExceptionFormat.Format(ex));
                    ProcessFactory.KillCurrent();
                }

                throw;
            }
        }
        else
        {
            UnpackagedStore!.Set(key, value);
        }
    }

    public static void SetIf<T>(bool condition, string key, T value)
    {
        if (condition)
        {
            Set(key, value);
        }
    }

    public static void SetIfNot<T>(bool condition, string key, T value)
    {
        if (!condition)
        {
            Set(key, value);
        }
    }

    public static T Update<T>(string key, T defaultValue, Func<T, T> modifier)
    {
        Debug.Assert(SupportedTypes.Contains(typeof(T)));
        T oldValue = Get(key, defaultValue);
        Set(key, modifier(oldValue));
        return oldValue;
    }

    public static T Update<T>(string key, T defaultValue, T newValue)
    {
        Debug.Assert(SupportedTypes.Contains(typeof(T)));
        T oldValue = Get(key, defaultValue);
        Set(key, newValue);
        return oldValue;
    }

    private sealed class FileSettingsStore
    {
        private static readonly string SettingsFilePath;
        private static readonly Lock SyncRoot = new();
        private readonly Dictionary<string, object?> cache;

        static FileSettingsStore()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directory = Path.Combine(appData, "SnapHutaoRemastered");
            Directory.CreateDirectory(directory);
            SettingsFilePath = Path.Combine(directory, "settings.json");
        }

        public FileSettingsStore()
        {
            cache = LoadFile();
        }

        public T Get<T>(string key, T defaultValue)
        {
            lock (SyncRoot)
            {
                if (cache.TryGetValue(key, out object? value))
                {
                    if (value is null)
                    {
                        return defaultValue;
                    }

                    if (typeof(T) == typeof(ApplicationDataCompositeValue))
                    {
                        return (T)(object)ConvertToCompositeValue(value);
                    }

                    if (value is T typedValue)
                    {
                        return typedValue;
                    }

                    // Type mismatch (e.g. loaded as different numeric type), try to convert
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                // Key not found: save default and return it
                cache[key] = defaultValue;
                SaveFile();
                return defaultValue;
            }
        }

        public void Set<T>(string key, T value)
        {
            lock (SyncRoot)
            {
                if (value is ApplicationDataCompositeValue composite)
                {
                    cache[key] = ConvertFromCompositeValue(composite);
                }
                else
                {
                    cache[key] = value;
                }

                SaveFile();
            }
        }

        private static Dictionary<string, object?> LoadFile()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    Dictionary<string, JsonElement>? raw = JsonSerializer.Deserialize(json, LocalSettingsJsonContext.Default.DictionaryStringJsonElement);
                    if (raw is null)
                    {
                        return [];
                    }

                    Dictionary<string, object?> result = [];
                    foreach (KeyValuePair<string, JsonElement> kvp in raw)
                    {
                        result[kvp.Key] = ConvertFromJsonElement(kvp.Value);
                    }

                    return result;
                }
            }
            catch
            {
                // Ignore deserialization errors, start fresh
            }

            return [];
        }

        private void SaveFile()
        {
            string tempFile = SettingsFilePath + ".tmp";
            try
            {
                string json = LocalSettingsJson.Serialize(cache, LocalSettingPlatformJson.Write);
                File.WriteAllText(tempFile, json);
                File.Move(tempFile, SettingsFilePath, overwrite: true);
            }
            catch
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }

        private static object? ConvertFromJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out int i) ? (object?)i : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => ConvertFromJsonObject(element),
                _ => null,
            };
        }

        private static Dictionary<string, object?> ConvertFromJsonObject(JsonElement element)
        {
            Dictionary<string, object?> result = [];
            foreach (JsonProperty property in element.EnumerateObject())
            {
                result[property.Name] = ConvertFromJsonElement(property.Value);
            }

            return result;
        }

        private static ApplicationDataCompositeValue ConvertToCompositeValue(object value)
        {
            ApplicationDataCompositeValue composite = [];
            if (value is Dictionary<string, object?> dict)
            {
                foreach (KeyValuePair<string, object?> kvp in dict)
                {
                    composite[kvp.Key] = kvp.Value;
                }
            }

            return composite;
        }

        private static Dictionary<string, object?> ConvertFromCompositeValue(ApplicationDataCompositeValue composite)
        {
            Dictionary<string, object?> dict = [];
            foreach (KeyValuePair<string, object> kvp in composite)
            {
                dict[kvp.Key] = kvp.Value;
            }

            return dict;
        }
    }
}
