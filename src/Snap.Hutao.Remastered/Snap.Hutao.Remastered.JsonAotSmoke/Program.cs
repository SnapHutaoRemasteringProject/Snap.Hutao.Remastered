// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

global using System;
global using System.Collections.Generic;
global using System.Text.Json;
global using System.Text.Json.Serialization;

using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Core.Text.Json.Converter;
using Snap.Hutao.Remastered.Service.AI.Migration;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

// A managed execution of the same source is insufficient evidence for this check.
if (JsonSerializer.IsReflectionEnabledByDefault || RuntimeFeature.IsDynamicCodeSupported || RuntimeFeature.IsDynamicCodeCompiled)
{
    throw new InvalidOperationException("Publish and run this project with Native AOT and JSON reflection disabled.");
}

JsonSerializerOptions options = JsonWireOptions.Create();
options.TypeInfoResolver = JsonTypeInfoResolver.Combine(DiagnosticsJsonContext.Default, AgentJsonContext.Default);
options.Converters.Add(new ObjectDictionaryConverter());
options.WriteIndented = false;

Dictionary<string, object> payload = new()
{
    ["deviceName"] = "胡桃",
    ["proxyStatus"] = 0,
    ["appUpdateTimeDiff"] = 1702604034482L,
};
string json = JsonSerialize.Serialize(payload, options);
if (json != """{"deviceName":"胡桃","proxyStatus":0,"appUpdateTimeDiff":1702604034482}""")
{
    throw new InvalidOperationException("The fingerprint dictionary wire format changed.");
}

Dictionary<string, JsonElement>? restored = JsonSerialize.Deserialize<Dictionary<string, JsonElement>>(json, options);
if (restored?["appUpdateTimeDiff"].GetInt64() != 1702604034482L)
{
    throw new InvalidOperationException("The integer value changed during the round trip.");
}

if (!ReferenceEquals(options, JsonSerialize.GetTypeInfo<Dictionary<string, object>>(options).Options))
{
    throw new InvalidOperationException("The generated contract used a different options instance.");
}

using MemoryStream stream = new();
await JsonSerialize.SerializeAsync(stream, payload, options);
stream.Position = 0;
restored = await JsonSerialize.DeserializeAsync<Dictionary<string, JsonElement>>(stream, options);
if (restored?["deviceName"].GetString() != "胡桃")
{
    throw new InvalidOperationException("The asynchronous JSON round trip failed.");
}

// These files used the serializer defaults, so keep their strict numeric handling.
List<LegacyModelProfile>? profiles = JsonSerialize.Deserialize<List<LegacyModelProfile>>("""[{"Name":"provider","MaxOutputTokens":8192}]""", AgentJsonContext.Default.Options);
if (profiles is not [{ Name: "provider", MaxOutputTokens: 8192 }])
{
    throw new InvalidOperationException("The legacy profile list could not be read.");
}

List<LegacyConversation>? conversations = JsonSerialize.Deserialize<List<LegacyConversation>>("""[{"Title":"chat","Messages":[{"Role":"user","Content":"question"}]}]""", AgentJsonContext.Default.Options);
if (conversations is not [{ Title: "chat", Messages: [{ Content: "question" }] }])
{
    throw new InvalidOperationException("The legacy conversation list could not be read.");
}

try
{
    JsonSerialize.Deserialize<List<LegacyModelProfile>>("""[{"MaxOutputTokens":"8192"}]""", AgentJsonContext.Default.Options);
    throw new InvalidOperationException("Legacy files unexpectedly accepted string-encoded numbers.");
}
catch (JsonException)
{
    // Expected: the legacy serializer contract is strict.
}

try
{
    JsonSerialize.Serialize(new UnregisteredPayload(), options);
    throw new InvalidOperationException("An unregistered contract unexpectedly enabled a fallback.");
}
catch (NotSupportedException)
{
    // Expected: the generated-only resolver does not discover arbitrary runtime types.
}

if (!options.IsReadOnly)
{
    throw new InvalidOperationException("JSON options were not frozen on first use.");
}

Console.WriteLine("PASS: Native AOT; JSON reflection and dynamic code disabled; generated contracts, dictionary, async and legacy file formats verified.");

internal sealed class UnregisteredPayload;
