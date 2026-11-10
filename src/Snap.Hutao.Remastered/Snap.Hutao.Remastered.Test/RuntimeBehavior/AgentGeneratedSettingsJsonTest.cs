using Microsoft.Extensions.AI;
using Snap.Hutao.Remastered.Core.AI;
using Snap.Hutao.Remastered.Core.Text.Json;
using Snap.Hutao.Remastered.Service.AI.Models;
using Snap.Hutao.Remastered.Service.AI.Settings;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Test.RuntimeBehavior;

[TestClass]
public sealed class AgentGeneratedSettingsJsonTest
{
    [TestMethod]
    public void GeneratedSettingsPreserveProfilesSelectionOptionsAndCredentialExclusion()
    {
        AgentSettings settings = new();
        ModelProfile model = new() { Name = "中文 <&>", ModelId = "test-model" };
        model.AgentOptions.Temperature = 0.25f;
        model.AgentOptions.MaxOutputTokens = 4321;
        model.AgentOptions.ReasoningEffort = EnumBox.Of(ReasoningEffort.High);
        ModelProviderProfile provider = new()
        {
            Name = "test-provider",
            Endpoint = "https://example.invalid/v1",
            ApiKey = "must-not-be-persisted"
        };
        provider.ModelProfiles.Add(model);
        provider.SelectedModelProfileId = model.Id;
        settings.ModelProviderProfiles.Add(provider);
        settings.SelectedModelProviderProfileId = provider.Id;
        JsonSerializerOptions options = Options(AgentSettingsJsonContext.Default);
        string json = JsonSerialize.Serialize(settings, options);
        Assert.IsFalse(json.Contains("must-not-be-persisted", StringComparison.Ordinal));
        AgentSettings actual = JsonSerialize.Deserialize<AgentSettings>(json, options)!;
        Assert.AreEqual(provider.Id, actual.SelectedModelProviderProfileId);
        ModelProviderProfile actualProvider = actual.ModelProviderProfiles.Single();
        Assert.AreEqual(model.Id, actualProvider.SelectedModelProfileId);
        ModelProfile actualModel = actualProvider.ModelProfiles.Single();
        Assert.AreEqual(model.Name, actualModel.Name);
        Assert.AreEqual(0.25f, actualModel.AgentOptions.Temperature);
        Assert.AreEqual(4321, actualModel.AgentOptions.MaxOutputTokens);
        Assert.AreEqual(ReasoningEffort.High, actualModel.AgentOptions.ReasoningEffort!.Value);
        Assert.IsNull(actualProvider.ApiKey);
    }

    [TestMethod]
    public void GeneratedSettingsRetainStrictNumbers()
    {
        const string json = """{"ModelProviderProfiles":[{"ModelProfiles":[{"AgentOptions":{"Temperature":"0.25"}}]}]}""";
        Assert.ThrowsExactly<JsonException>(() => JsonSerialize.Deserialize<AgentSettings>(json, Options(AgentSettingsJsonContext.Default)));
    }

    [TestMethod]
    public void GeneratedSettingsMatchPreviousReflectionWireFormat()
    {
        AgentSettings settings = new();
        settings.ModelProviderProfiles.Add(new() { ModelProfiles = [new() { ModelId = "test" }] });
        JsonSerializerOptions legacy = Options(new DefaultJsonTypeInfoResolver());
        string oldJson = JsonSerializer.Serialize(settings, legacy);
        string newJson = JsonSerialize.Serialize(settings, Options(AgentSettingsJsonContext.Default));
        Assert.AreEqual(oldJson, newJson);
        Assert.AreEqual(oldJson, JsonSerializer.Serialize(JsonSerialize.Deserialize<AgentSettings>(oldJson, Options(AgentSettingsJsonContext.Default)), legacy));
    }

    private static JsonSerializerOptions Options(IJsonTypeInfoResolver resolver) => new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        TypeInfoResolver = resolver
    };
}
