// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json.Nodes;

namespace Snap.Hutao.Remastered.Core.Text.Json.Converter;

/// <summary>
/// Serializes the small <see cref="object"/>-valued dictionaries used by the fingerprint
/// payload and the JS bridge without asking the serializer for runtime type metadata.
/// </summary>
internal sealed class ObjectDictionaryConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);

        Dictionary<string, object> result = [];
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            result[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString()!,
                JsonValueKind.Number => property.Value.TryGetInt64(out long value) ? value : property.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => property.Value.Clone(),
            };
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<string, object> item in value)
        {
            writer.WritePropertyName(item.Key);
            WriteValue(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case bool boolean:
                writer.WriteBooleanValue(boolean);
                break;
            case int int32:
                writer.WriteNumberValue(int32);
                break;
            case long int64:
                writer.WriteNumberValue(int64);
                break;
            case uint uint32:
                writer.WriteNumberValue(uint32);
                break;
            case ulong uint64:
                writer.WriteNumberValue(uint64);
                break;
            case double double64:
                writer.WriteNumberValue(double64);
                break;
            case float single:
                writer.WriteNumberValue(single);
                break;
            case decimal decimal128:
                writer.WriteNumberValue(decimal128);
                break;
            case JsonElement element:
                element.WriteTo(writer);
                break;
            case JsonNode node:
                node.WriteTo(writer);
                break;
            default:
                throw new NotSupportedException($"The object-valued dictionary entry type '{value.GetType()}' is not supported by the AOT JSON path.");
        }
    }
}
