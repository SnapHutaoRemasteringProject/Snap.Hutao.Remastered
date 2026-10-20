// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json.Converter;
using System.Text.Encodings.Web;

namespace Snap.Hutao.Remastered.Core.Text.Json;

internal static class JsonWireOptions
{
    // Generated contexts share the existing wire policy without initializing a reflection resolver.
    public static JsonSerializerOptions Create() => new()
    {
        AllowOutOfOrderMetadataProperties = true,
        AllowTrailingCommas = true,
        Converters = { new InternStringConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        WriteIndented = true,
    };
}
