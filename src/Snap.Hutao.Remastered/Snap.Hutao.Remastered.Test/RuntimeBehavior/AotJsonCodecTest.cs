using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Core.Text.Json.Annotation;
using Snap.Hutao.Remastered.Model.Primitive.Converter;
using Snap.Hutao.Remastered.Core.Text.Json.Converter;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class AotJsonCodecTest
{
    [TestMethod]
    public void ClosedEnumConvertersWorkWithGeneratedMetadata()
    {
        var context = new AotJsonCodecTestContext(JsonWireOptions.Create());
        AotJsonEnumProbe? value = JsonSerializer.Deserialize("""{"NumberString":"1","Name":"oNe","Asymmetric":1} """, context.AotJsonEnumProbe);
        Assert.IsNotNull(value);
        Assert.AreEqual(AotJsonTestEnum.One, value.NumberString);
        Assert.AreEqual(AotJsonTestEnum.One, value.Name);
        using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(value, context.AotJsonEnumProbe));
        Assert.AreEqual("1", json.RootElement.GetProperty("NumberString").GetString());
        Assert.AreEqual("One", json.RootElement.GetProperty("Name").GetString());
        Assert.AreEqual("1", json.RootElement.GetProperty("Asymmetric").GetString());
    }

    [TestMethod]
    public void UnknownEnumNumbersKeepTheirWireRepresentation()
    {
        var context = new AotJsonCodecTestContext(JsonWireOptions.Create());
        AotJsonEnumProbe? value = JsonSerializer.Deserialize("""{"NumberString":"429","Name":"One","Asymmetric":429} """, context.AotJsonEnumProbe);
        Assert.IsNotNull(value);
        using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(value, context.AotJsonEnumProbe));
        Assert.AreEqual("429", json.RootElement.GetProperty("NumberString").GetString());
        Assert.AreEqual("429", json.RootElement.GetProperty("Asymmetric").GetString());
    }

    [TestMethod]
    public void IdentityCodecHonorsStrictNumbers()
    {
        var context = new AotJsonCodecTestContext(new JsonSerializerOptions());
        Assert.AreEqual(42U, JsonSerializer.Deserialize("42", context.AotJsonTestIdentity).Value);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("\"42\"", context.AotJsonTestIdentity));
        Assert.AreEqual("42", JsonSerializer.Serialize(new AotJsonTestIdentity(42), context.AotJsonTestIdentity));
    }

    [TestMethod]
    public void IdentityCodecHonorsNumberHandlingWithoutReflection()
    {
        var context = new AotJsonCodecTestContext(new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
        });
        Assert.AreEqual(uint.MaxValue, JsonSerializer.Deserialize("\"4294967295\"", context.AotJsonTestIdentity).Value);
        Assert.AreEqual("\"4294967295\"", JsonSerializer.Serialize(new AotJsonTestIdentity(uint.MaxValue), context.AotJsonTestIdentity));
    }

    [TestMethod]
    public void IdentityCodecRetainsCallerUIntConverter()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new OffsetUIntConverter());
        var context = new AotJsonCodecTestContext(options);
        Assert.AreEqual(43U, JsonSerializer.Deserialize("42", context.AotJsonTestIdentity).Value);
        Assert.AreEqual("42", JsonSerializer.Serialize(new AotJsonTestIdentity(43), context.AotJsonTestIdentity));
    }

    private sealed class OffsetUIntConverter : JsonConverter<uint>
    {
        public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetUInt32() + 1;
        public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options) => writer.WriteNumberValue(value - 1);
    }
}

public enum AotJsonTestEnum { None, One }

public sealed class AotJsonEnumProbe
{
    [JsonConverter(typeof(NumberStringEnumConverter<AotJsonTestEnum>))]
    public AotJsonTestEnum NumberString { get; set; }

    [JsonConverter(typeof(NameEnumConverter<AotJsonTestEnum>))]
    public AotJsonTestEnum Name { get; set; }

    [JsonConverter(typeof(AotNumberToStringEnumConverter))]
    public AotJsonTestEnum Asymmetric { get; set; }
}

[JsonConverter(typeof(IdentityConverter<AotJsonTestIdentity>))]
public readonly struct AotJsonTestIdentity(uint value)
{
    public uint Value { get; } = value;
}

public sealed class AotNumberToStringEnumConverter() : UnsafeEnumConverter<AotJsonTestEnum>(JsonEnumHandling.Number, JsonEnumHandling.NumberString);

[JsonSerializable(typeof(AotJsonEnumProbe))]
[JsonSerializable(typeof(AotJsonTestIdentity))]
[JsonSerializable(typeof(uint))]
internal sealed partial class AotJsonCodecTestContext : JsonSerializerContext;
