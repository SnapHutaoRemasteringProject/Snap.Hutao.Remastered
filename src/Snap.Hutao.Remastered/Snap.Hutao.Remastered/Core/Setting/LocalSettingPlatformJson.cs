// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Windows.Storage;

namespace Snap.Hutao.Remastered.Core.Setting;

internal static class LocalSettingPlatformJson
{
    public static void Write(Utf8JsonWriter writer, object value)
    {
        switch (value)
        {
            case Windows.Foundation.Point point:
                JsonSerializer.Serialize(writer, point, LocalSettingPlatformJsonContext.Default.Point);
                break;
            case Windows.Foundation.Size size:
                JsonSerializer.Serialize(writer, size, LocalSettingPlatformJsonContext.Default.Size);
                break;
            case Windows.Foundation.Rect rect:
                JsonSerializer.Serialize(writer, rect, LocalSettingPlatformJsonContext.Default.Rect);
                break;
            case ApplicationDataCompositeValue composite:
                LocalSettingsJson.WriteObject(writer, composite.ToDictionary(static item => item.Key, static item => (object?)item.Value), Write);
                break;
            default:
                throw new NotSupportedException("Unsupported local setting value type.");
        }
    }
}

[JsonSerializable(typeof(Windows.Foundation.Point))]
[JsonSerializable(typeof(Windows.Foundation.Size))]
[JsonSerializable(typeof(Windows.Foundation.Rect))]
internal sealed partial class LocalSettingPlatformJsonContext : JsonSerializerContext;
