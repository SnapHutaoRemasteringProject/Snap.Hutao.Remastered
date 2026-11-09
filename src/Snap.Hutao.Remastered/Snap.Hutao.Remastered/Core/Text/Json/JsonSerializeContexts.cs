// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json.Converter;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Model.Primitive.Converter;
using Snap.Hutao.Remastered.Web.Hutao;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Core.Text.Json;

/// <summary>
/// Combines the generated resolvers on the caller's options. The source generator's
/// IJsonTypeInfoResolver implementation creates metadata for the options it receives;
/// constructing a context with those options would instead freeze and replace their resolver.
/// </summary>
internal static class JsonSerializeContexts
{
    public static IJsonTypeInfoResolver Generated { get; } = JsonTypeInfoResolver.Combine(
        InterProcessJsonContext.Default,
        DiagnosticsJsonContext.Default,
        UserJsonContext.Default,
        UigfJsonContext.Default,
        UiafJsonContext.Default,
        GamePackageJsonContext.Default,
        GameDataJsonContext.Default,
        BridgeJsonContext.Default,
        CacheJsonContext.Default,
        AgentJsonContext.Default,
        InfrastructureJsonContext.Default);

    public static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = JsonWireOptions.Create();
        AddConverters(options);
        options.TypeInfoResolver = Generated;
        return options;
    }

    public static void AddConverters(JsonSerializerOptions options)
    {
        // IdentityGenerator's converter attributes are not visible to a different generator
        // in the same round, so every closed identity converter is registered explicitly.
        options.Converters.Add(new IdentityConverter<AvatarId>());
        options.Converters.Add(new IdentityConverter<CostumeId>());
        options.Converters.Add(new IdentityConverter<FetterLevel>());
        options.Converters.Add(new IdentityConverter<HardChallengeScheduleId>());
        options.Converters.Add(new IdentityConverter<Level>());
        options.Converters.Add(new IdentityConverter<MainQuestId>());
        options.Converters.Add(new IdentityConverter<MonsterId>());
        options.Converters.Add(new IdentityConverter<PromoteLevel>());
        options.Converters.Add(new IdentityConverter<ProudSkillId>());
        options.Converters.Add(new IdentityConverter<ReliquaryId>());
        options.Converters.Add(new IdentityConverter<ReliquarySetId>());
        options.Converters.Add(new IdentityConverter<RoleCombatScheduleId>());
        options.Converters.Add(new IdentityConverter<SkillId>());
        options.Converters.Add(new IdentityConverter<SkillLevel>());
        options.Converters.Add(new IdentityConverter<WeaponId>());
        options.Converters.Add(new ObjectDictionaryConverter());
    }
}
