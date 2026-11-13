// Copyright (c) Snap Hutao RP. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class ProductionHttpModelJsonTest
{
    [DataTestMethod]
    [DataRow("Offering", WorldExplorationType.Offering)]
    [DataRow("reputation", WorldExplorationType.Reputation)]
    [DataRow("TypeUnknow", WorldExplorationType.TypeUnknow)]
    public void ActualApiModelReadsStringEnumsWithGeneratedMetadata(string value, WorldExplorationType expected)
    {
        string json = $$"""{"type":"{{value}}","name":"测试","offerings":[{"name":"供奉","level":3,"icon":"https://example.invalid/icon.png"}]}""";
        WorldExploration model = JsonSerializer.Deserialize(json, ProductionHttpModelJsonContext.Default.WorldExploration)!;

        Assert.AreEqual(expected, model.Type);
        Assert.AreEqual("测试", model.Name);
        Assert.AreEqual(3, model.Offerings[0].Level);
        using JsonDocument result = JsonDocument.Parse(JsonSerializer.Serialize(model, ProductionHttpModelJsonContext.Default.WorldExploration));
        Assert.AreEqual(expected.ToString(), result.RootElement.GetProperty("type").GetString());
    }

    [TestMethod]
    public void NumericEnumCompatibilityIsRetained()
    {
        WorldExploration model = JsonSerializer.Deserialize("""{"type":1}""", ProductionHttpModelJsonContext.Default.WorldExploration)!;
        Assert.AreEqual(WorldExplorationType.Reputation, model.Type);
    }
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(WorldExploration))]
internal sealed partial class ProductionHttpModelJsonContext : JsonSerializerContext;
