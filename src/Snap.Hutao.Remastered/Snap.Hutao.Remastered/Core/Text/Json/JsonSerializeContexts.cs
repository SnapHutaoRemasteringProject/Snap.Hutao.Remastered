// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json.Converter;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Model.Primitive.Converter;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Core.Text.Json;

/// <summary>
/// Holds the per-domain generated contexts and looks up metadata directly. The contexts are
/// deliberately not installed into <see cref="JsonSerializerOptions.TypeInfoResolver"/>: a
/// <see cref="JsonSerializerContext"/> freezes the options passed to its constructor, so several
/// contexts cannot share one caller options instance, and metadata from a context built from a
/// clone fails STJ's "JsonTypeInfo.Options must match" validation.
/// </summary>
internal static class JsonSerializeContexts
{
    private static readonly ConditionalWeakTable<JsonSerializerOptions, ContextSet> Contexts = new();

    public static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = JsonWireOptions.Create();
        AddConverters(options);
        return options;
    }

    public static JsonTypeInfo? TryGetTypeInfo(Type type, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(options);

        return Contexts.GetValue(options, static value => new ContextSet(value)).TryGetTypeInfo(type);
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

    private static JsonSerializerOptions Clone(JsonSerializerOptions options)
    {
        return new(options) { TypeInfoResolver = null };
    }

    private sealed class ContextSet
    {
        private readonly JsonSerializerContext[] contexts;

        public ContextSet(JsonSerializerOptions options)
        {
            // Each JsonSerializerContext freezes its options, so each context gets its own
            // snapshot. Formatting and converters still match the caller's options.
            contexts =
            [
                new InterProcessJsonContext(Clone(options)),
                new DiagnosticsJsonContext(Clone(options)),
                new UserJsonContext(Clone(options)),
                new UigfJsonContext(Clone(options)),
                new UiafJsonContext(Clone(options)),
                new GamePackageJsonContext(Clone(options)),
                new GameDataJsonContext(Clone(options)),
                new BridgeJsonContext(Clone(options)),
                new CacheJsonContext(Clone(options)),
                new AgentJsonContext(Clone(options)),
            ];
        }

        public JsonTypeInfo? TryGetTypeInfo(Type type)
        {
            foreach (JsonSerializerContext context in contexts)
            {
                try
                {
                    if (context.GetTypeInfo(type) is { } typeInfo)
                    {
                        return typeInfo;
                    }
                }
                catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException or ArgumentException)
                {
                    // This context does not own the type.
                }
            }

            return null;
        }
    }
}
