// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json;
using System.Collections.Concurrent;
using System.IO;
using Windows.Storage;

namespace Snap.Hutao.Remastered.Core.Diagnostics;

public sealed class DiagnosticsSettingsProvider
{
    private static readonly string? SettingsFilePath;
    private static readonly JsonSerializerOptions CompactJsonOptions = JsonOptions.CreateCompact();

    private readonly ConcurrentDictionary<string, object?>? store;

    static DiagnosticsSettingsProvider()
    {
        if (RuntimeEnvironment.IsUnpackaged)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directory = Path.Combine(appData, "SnapHutaoRemastered");
            Directory.CreateDirectory(directory);
            SettingsFilePath = Path.Combine(directory, "diagnostics_settings.json");
        }
    }

    public DiagnosticsSettingsProvider()
    {
        if (RuntimeEnvironment.IsUnpackaged && SettingsFilePath is not null)
        {
            store = LoadFile();
        }
    }

    public object? GetValue(string key)
    {
        if (RuntimeEnvironment.IsPackaged)
        {
            return ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out object? value) ? value : null;
        }

        return store?.TryGetValue(key, out object? v) is true ? v : null;
    }

    public void SetValue(string key, object? value)
    {
        if (RuntimeEnvironment.IsPackaged)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
            return;
        }

        store![key] = value;
        SaveFile();
    }

    public bool RemoveValue(string key)
    {
        if (RuntimeEnvironment.IsPackaged)
        {
            return ApplicationData.Current.LocalSettings.Values.Remove(key);
        }

        bool removed = store!.TryRemove(key, out _);
        if (removed)
        {
            SaveFile();
        }

        return removed;
    }

    private static ConcurrentDictionary<string, object?> LoadFile()
    {
        try
        {
            if (SettingsFilePath is not null && File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                Dictionary<string, JsonElement>? raw = JsonSerialize.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions.Default);
                if (raw is not null)
                {
                    ConcurrentDictionary<string, object?> result = [];
                    foreach (KeyValuePair<string, JsonElement> kvp in raw)
                    {
                        result[kvp.Key] = ConvertFromJsonElement(kvp.Value);
                    }

                    return result;
                }
            }
        }
        catch
        {
            // Ignore, start fresh
        }

        return [];
    }

    private void SaveFile()
    {
        if (SettingsFilePath is null || store is null)
        {
            return;
        }

        try
        {
            Dictionary<string, object?> snapshot = store.ToDictionary(static item => item.Key, static item => item.Value);
            string json = JsonSerialize.Serialize(snapshot, CompactJsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Best effort
        }
    }

    private static object? ConvertFromJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out int i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null,
        };
    }
}
