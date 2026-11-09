// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Core.Text.Json.Converter;
using System.IO;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class AotJsonSerializationTest
{
    [TestMethod]
    public void GeneratedResolversShareCallerOptionsAndNestedContracts()
    {
        JsonSerializerOptions options = CreateOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.WriteIndented = false;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;

        const string json = """{"text":"胡桃","number":"42","external":{"id":"7"}}""";
        GeneratedJsonPayload payload = JsonSerialize.Deserialize<GeneratedJsonPayload>(json, options)!;

        Assert.AreEqual(42U, payload.Number);
        Assert.AreEqual(7, payload.External!.Id);
        Assert.AreEqual(json, JsonSerialize.Serialize(payload, options));
        Assert.AreSame(options, JsonSerialize.GetTypeInfo<GeneratedJsonPayload>(options).Options);
        Assert.AreSame(options, JsonSerialize.GetTypeInfo<ExternalJsonContract>(options).Options);
        Assert.AreSame(JsonSerialize.GetTypeInfo<GeneratedJsonPayload>(options), JsonSerialize.GetTypeInfo<GeneratedJsonPayload>(options));
    }

    [TestMethod]
    public void FirstUseFreezesOptionsAndClonesKeepTheirOwnConfiguration()
    {
        JsonSerializerOptions options = CreateOptions();
        GeneratedJsonPayload payload = new() { Text = "胡桃", Number = 42 };
        Assert.Contains('\n', JsonSerialize.Serialize(payload, options));
        Assert.IsTrue(options.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => options.WriteIndented = false);

        JsonSerializerOptions compact = new(options) { WriteIndented = false };
        Assert.DoesNotContain('\n', JsonSerialize.Serialize(payload, compact));
        Assert.AreSame(compact, JsonSerialize.GetTypeInfo<GeneratedJsonPayload>(compact).Options);
    }

    [TestMethod]
    public void ExplicitConverterOverridesGeneratedContract()
    {
        JsonSerializerOptions options = CreateOptions();
        options.Converters.Add(new GeneratedPayloadConverter());

        Assert.AreEqual("\"custom\"", JsonSerialize.Serialize(new GeneratedJsonPayload(), options));
        Assert.AreEqual("custom", JsonSerialize.Deserialize<GeneratedJsonPayload>("\"custom\"", options)!.Text);
    }

    [TestMethod]
    public void MissingMetadataFailsWithoutReflectionFallback()
    {
        JsonSerializerOptions options = CreateOptions();

        Assert.Throws<NotSupportedException>(() => JsonSerialize.Serialize(new UnregisteredJsonPayload(), options));
        Assert.Throws<NotSupportedException>(() => JsonSerialize.Deserialize<UnregisteredJsonPayload>("{}", options));
    }

    [TestMethod]
    public void GeneratedDictionaryContractWritesFingerprintValuesWithoutReflection()
    {
        JsonSerializerOptions options = JsonWireOptions.Create();
        options.TypeInfoResolver = DiagnosticsJsonContext.Default;
        options.Converters.Add(new ObjectDictionaryConverter());
        options.WriteIndented = false;

        Dictionary<string, object> values = new()
        {
            ["deviceName"] = "胡桃",
            ["proxyStatus"] = 0,
            ["appUpdateTimeDiff"] = 1702604034482L,
            ["enabled"] = true,
        };
        Assert.AreEqual("""{"deviceName":"胡桃","proxyStatus":0,"appUpdateTimeDiff":1702604034482,"enabled":true}""", JsonSerialize.Serialize(values, options));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public void ResolverFailuresArePreserved(int kind)
    {
        Exception expected = kind switch
        {
            0 => new NotSupportedException("unsupported contract"),
            1 => new InvalidOperationException("invalid contract"),
            _ => new ArgumentException("invalid options"),
        };
        JsonSerializerOptions options = new() { TypeInfoResolver = new ThrowingJsonResolver(expected) };

        Exception actual = Assert.Throws<Exception>(() => JsonSerialize.Serialize(new GeneratedJsonPayload(), options));
        Assert.AreSame(expected, actual);
    }

    [TestMethod]
    public void DeclaredTypeAndStrictReadPolicyArePreserved()
    {
        JsonSerializerOptions options = CreateOptions();
        GeneratedJsonPayload payload = new DerivedGeneratedJsonPayload { Text = "base", Derived = "must not be serialized" };

        Assert.DoesNotContain("Derived", JsonSerialize.Serialize(payload, options));

        JsonSerializerOptions strict = new(options) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        Assert.Throws<JsonException>(() => JsonSerialize.Deserialize<GeneratedJsonPayload>("""{"Unknown":1}""", strict));
    }

    [TestMethod]
    public void SynchronousOverloadsUseTheSameGeneratedContract()
    {
        JsonSerializerOptions options = CreateOptions();
        GeneratedJsonPayload payload = new() { Text = "胡桃", Number = 42 };
        string json = JsonSerialize.Serialize(payload, options);
        byte[] bytes = JsonSerialize.SerializeToUtf8Bytes(payload, options);

        Assert.AreEqual(json, Encoding.UTF8.GetString(bytes));
        Assert.AreEqual(json, JsonSerialize.Serialize(payload, JsonSerialize.GetTypeInfo<GeneratedJsonPayload>(options)));
        Assert.AreEqual(42U, JsonSerialize.Deserialize<GeneratedJsonPayload>(bytes.AsSpan(), options)!.Number);

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.AreEqual("胡桃", JsonSerialize.Deserialize<GeneratedJsonPayload>(document.RootElement, options)!.Text);

        Utf8JsonReader reader = new(bytes);
        Assert.AreEqual(42U, JsonSerialize.Deserialize<GeneratedJsonPayload>(ref reader, options)!.Number);

        using MemoryStream stream = new();
        JsonSerialize.Serialize(stream, payload, options);
        stream.Position = 0;
        Assert.AreEqual("胡桃", JsonSerialize.Deserialize<GeneratedJsonPayload>(stream, options)!.Text);

        using MemoryStream writerStream = new();
        using (Utf8JsonWriter writer = new(writerStream))
        {
            JsonSerialize.Serialize(writer, payload, options);
        }
        using JsonDocument written = JsonDocument.Parse(writerStream.ToArray());
        Assert.AreEqual(42U, written.RootElement.GetProperty("Number").GetUInt32());
    }

    [TestMethod]
    public async Task AsynchronousOverloadsUseGeneratedContractsAndCancellation()
    {
        JsonSerializerOptions options = CreateOptions();
        GeneratedJsonPayload payload = new() { Text = "胡桃", Number = 42 };
        using MemoryStream stream = new();
        await JsonSerialize.SerializeAsync(stream, payload, options);
        stream.Position = 0;
        Assert.AreEqual("胡桃", (await JsonSerialize.DeserializeAsync<GeneratedJsonPayload>(stream, options))!.Text);

        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await JsonSerialize.SerializeAsync(stream, payload, options, cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await JsonSerialize.DeserializeAsync<GeneratedJsonPayload>(stream, options, cancellation.Token));
    }

    [TestMethod]
    public void UnconfiguredOptionsRequireAnExplicitResolver()
    {
        JsonSerializerOptions options = new() { WriteIndented = false };
        Assert.Throws<InvalidOperationException>(() => JsonSerialize.Serialize(new UnregisteredJsonPayload(), options));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = JsonWireOptions.Create();
        options.TypeInfoResolver = JsonTypeInfoResolver.Combine(GeneratedPayloadJsonContext.Default, ExternalContractJsonContext.Default);
        return options;
    }

    private sealed class ThrowingJsonResolver(Exception exception) : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) => throw exception;
    }
}

internal class GeneratedJsonPayload
{
    public string? Text { get; set; }

    public uint Number { get; set; }

    public ExternalJsonValue? External { get; set; }
}

internal sealed class DerivedGeneratedJsonPayload : GeneratedJsonPayload
{
    public string? Derived { get; set; }
}

internal sealed class UnregisteredJsonPayload;

// The first context sees only this converter, not ExternalJsonContract. Its converter
// must therefore obtain the nested contract from the second resolver on the same options.
[JsonConverter(typeof(ExternalJsonValueConverter))]
internal sealed record ExternalJsonValue(int Id);

internal sealed record ExternalJsonContract(int Id);

internal sealed class ExternalJsonValueConverter : JsonConverter<ExternalJsonValue>
{
    public override ExternalJsonValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ExternalJsonContract? value = JsonSerializer.Deserialize(ref reader, (JsonTypeInfo<ExternalJsonContract>)options.GetTypeInfo(typeof(ExternalJsonContract)));
        return value is null ? null : new(value.Id);
    }

    public override void Write(Utf8JsonWriter writer, ExternalJsonValue value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new ExternalJsonContract(value.Id), (JsonTypeInfo<ExternalJsonContract>)options.GetTypeInfo(typeof(ExternalJsonContract)));
    }
}

internal sealed class GeneratedPayloadConverter : JsonConverter<GeneratedJsonPayload>
{
    public override GeneratedJsonPayload? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new() { Text = reader.GetString() };

    public override void Write(Utf8JsonWriter writer, GeneratedJsonPayload value, JsonSerializerOptions options) => writer.WriteStringValue("custom");
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(GeneratedJsonPayload))]
internal sealed partial class GeneratedPayloadJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ExternalJsonContract))]
internal sealed partial class ExternalContractJsonContext : JsonSerializerContext;
