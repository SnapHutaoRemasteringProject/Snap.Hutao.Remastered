// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Buffers;
using System.Globalization;
using System.Text;

namespace Snap.Hutao.Remastered.Core.Setting;

// The supported LocalSetting value contract is finite; serialization never discovers arbitrary runtime types.
internal static class LocalSettingsJson
{
    public static Dictionary<string, object?> Deserialize(string json)
    {
        Dictionary<string, JsonElement>? values = JsonSerializer.Deserialize(json, LocalSettingsJsonContext.Default.DictionaryStringJsonElement);
        return values?.ToDictionary(static item => item.Key, static item => ReadValue(item.Value)) ?? [];
    }

    public static string Serialize(IReadOnlyDictionary<string, object?> values, Action<Utf8JsonWriter, object>? writePlatformValue = null)
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            WriteObject(writer, values, writePlatformValue);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static void WriteObject(Utf8JsonWriter writer, IReadOnlyDictionary<string, object?> values, Action<Utf8JsonWriter, object>? writePlatformValue)
    {
        writer.WriteStartObject();
        foreach (KeyValuePair<string, object?> item in values)
        {
            writer.WritePropertyName(item.Key);
            WriteValue(writer, item.Value, writePlatformValue);
        }

        writer.WriteEndObject();
    }

    private static object? ReadValue(JsonElement element)
    {
        // Preserve the existing cache representation and legacy unsupported-array behavior.
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out int value) ? (object)value : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(static item => item.Name, static item => ReadValue(item.Value)),
            _ => null,
        };
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value, Action<Utf8JsonWriter, object>? writePlatformValue)
    {
        switch (value)
        {
            case null: writer.WriteNullValue(); break;
            case byte number: writer.WriteNumberValue(number); break;
            case short number: writer.WriteNumberValue(number); break;
            case ushort number: writer.WriteNumberValue(number); break;
            case int number: writer.WriteNumberValue(number); break;
            case uint number: writer.WriteNumberValue(number); break;
            case long number: writer.WriteNumberValue(number); break;
            case ulong number: writer.WriteNumberValue(number); break;
            case float number: writer.WriteNumberValue(number); break;
            case double number: writer.WriteNumberValue(number); break;
            case bool boolean: writer.WriteBooleanValue(boolean); break;
            case char character: writer.WriteStringValue(character.ToString()); break;
            case string text: writer.WriteStringValue(text); break;
            case DateTimeOffset timestamp: writer.WriteStringValue(timestamp); break;
            case TimeSpan duration: writer.WriteStringValue(duration.ToString("c", CultureInfo.InvariantCulture)); break;
            case Guid guid: writer.WriteStringValue(guid); break;
            case JsonElement element: element.WriteTo(writer); break;
            case IReadOnlyDictionary<string, object?> dictionary: WriteObject(writer, dictionary, writePlatformValue); break;
            default:
                if (writePlatformValue is null)
                {
                    throw new NotSupportedException("Unsupported local setting value type.");
                }

                writePlatformValue(writer, value);
                break;
        }
    }
}

[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
internal sealed partial class LocalSettingsJsonContext : JsonSerializerContext;
