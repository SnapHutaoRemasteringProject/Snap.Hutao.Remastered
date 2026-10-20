using Snap.Hutao.Remastered.Core.Setting;
using System.Globalization;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class LocalSettingsJsonTest
{
    [TestMethod]
    public void AllPortableSettingTypesKeepTheExistingWireFormat()
    {
        Dictionary<string, object?> values = new()
        {
            ["byte"] = byte.MaxValue, ["short"] = short.MinValue, ["ushort"] = ushort.MaxValue,
            ["int"] = int.MinValue, ["uint"] = uint.MaxValue, ["long"] = long.MinValue,
            ["ulong"] = ulong.MaxValue, ["float"] = 1.25F, ["double"] = double.MaxValue,
            ["bool"] = true, ["char"] = '胡', ["string"] = "胡桃<&>\"\\\n",
            ["date"] = new DateTimeOffset(2026, 10, 20, 12, 34, 56, TimeSpan.FromHours(8)),
            ["duration"] = TimeSpan.FromTicks(-123456789),
            ["guid"] = Guid.Parse("59114f3b-0bdf-4fdc-bb70-899c23ac11ad"), ["null"] = null,
            ["nested"] = new Dictionary<string, object?> { ["value"] = 3, ["null"] = null },
        };
        Assert.AreEqual(JsonSerializer.Serialize(values), LocalSettingsJson.Serialize(values));
    }

    [TestMethod]
    public void LegacySettingsKeepTheirCacheTypesAndNestedValues()
    {
        Dictionary<string, object?> values = LocalSettingsJson.Deserialize("""
            {"int":12,"double":1.25,"large":4294967295,"text":"旧配置","true":true,"false":false,
             "null":null,"nested":{"value":5,"null":null},"unsupportedArray":[1,2]}
            """);
        Assert.AreEqual(12, values["int"]);
        Assert.AreEqual(1.25D, values["double"]);
        Assert.AreEqual(4294967295D, values["large"]);
        Assert.AreEqual("旧配置", values["text"]);
        Assert.AreEqual(true, values["true"]);
        Assert.AreEqual(false, values["false"]);
        Assert.IsNull(values["null"]);
        Assert.IsNull(values["unsupportedArray"]);
        var nested = (Dictionary<string, object?>)values["nested"]!;
        Assert.AreEqual(5, nested["value"]);
        Assert.IsNull(nested["null"]);
    }

    [TestMethod]
    public void SerializedIntegerBoundariesAreNotRounded()
    {
        using JsonDocument json = JsonDocument.Parse(LocalSettingsJson.Serialize(new Dictionary<string, object?>
        {
            ["signed"] = long.MinValue, ["unsigned"] = ulong.MaxValue,
        }));
        Assert.AreEqual(long.MinValue, json.RootElement.GetProperty("signed").GetInt64());
        Assert.AreEqual(ulong.MaxValue, json.RootElement.GetProperty("unsigned").GetUInt64());
    }

    [TestMethod]
    public void JsonElementsAreWrittenWithoutReflection()
    {
        using JsonDocument element = JsonDocument.Parse("{\"nested\":42}");
        Assert.AreEqual("{\"value\":{\"nested\":42}}", LocalSettingsJson.Serialize(new Dictionary<string, object?> { ["value"] = element.RootElement }));
    }

    [TestMethod]
    public void PlatformValuesUseOnlyTheExplicitWriter()
    {
        int calls = 0;
        string json = LocalSettingsJson.Serialize(new Dictionary<string, object?> { ["value"] = new Version(1, 2) }, (writer, value) =>
        {
            calls++;
            writer.WriteStringValue(value.ToString());
        });
        Assert.AreEqual(1, calls);
        Assert.AreEqual("{\"value\":\"1.2\"}", json);
    }

    [TestMethod]
    public void UnsupportedObjectsCannotSilentlyUseReflection()
    {
        Assert.Throws<NotSupportedException>(() => LocalSettingsJson.Serialize(new Dictionary<string, object?> { ["value"] = new Version(1, 2) }));
    }

    [TestMethod]
    public void CorruptJsonIsReportedAndNullRootRemainsEmpty()
    {
        Assert.Throws<JsonException>(() => LocalSettingsJson.Deserialize("not JSON"));
        Assert.AreEqual(0, LocalSettingsJson.Deserialize("null").Count);
    }

    [TestMethod]
    public void CultureCannotChangeNumericOrDurationFormats()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Dictionary<string, object?> values = new() { ["number"] = 1.25D, ["duration"] = TimeSpan.FromSeconds(1.25) };
            Assert.AreEqual(JsonSerializer.Serialize(values), LocalSettingsJson.Serialize(values));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
