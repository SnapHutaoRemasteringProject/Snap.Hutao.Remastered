// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.HardChallenge;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.SpiralAbyss;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Model.Primitive.Converter;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Core.Text.Json;

// All five JsonTextValueConverter mappings and their nested types are registered below.
internal static class PersistenceJson
{
    private static readonly PersistenceJsonContext Context = new(CreateOptions());

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, GetTypeInfo<T>());

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize(json, GetTypeInfo<T>());

    private static JsonTypeInfo<T> GetTypeInfo<T>()
    {
        return Context.GetTypeInfo(typeof(T)) is JsonTypeInfo<T> info
            ? info
            : throw new NotSupportedException("The persisted JSON type must be registered in PersistenceJsonContext.");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        // Keep the existing wire policy, but the generated context is the only resolver for this path.
        JsonSerializerOptions options = JsonWireOptions.Create();
        // IdentityGenerator's converter attributes aren't visible to a different generator in the same round.
        // Register the actual closed converters instead of allowing empty partial structs to become JSON objects.
        options.Converters.Add(new IdentityConverter<AvatarId>());
        options.Converters.Add(new IdentityConverter<CostumeId>());
        options.Converters.Add(new IdentityConverter<FetterLevel>());
        options.Converters.Add(new IdentityConverter<HardChallengeScheduleId>());
        options.Converters.Add(new IdentityConverter<Level>());
        options.Converters.Add(new IdentityConverter<MonsterId>());
        options.Converters.Add(new IdentityConverter<PromoteLevel>());
        options.Converters.Add(new IdentityConverter<ReliquaryId>());
        options.Converters.Add(new IdentityConverter<ReliquarySetId>());
        options.Converters.Add(new IdentityConverter<RoleCombatScheduleId>());
        options.Converters.Add(new IdentityConverter<SkillId>());
        options.Converters.Add(new IdentityConverter<SkillLevel>());
        options.Converters.Add(new IdentityConverter<WeaponId>());
        return options;
    }
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(DetailedCharacter))]
[JsonSerializable(typeof(DailyNote))]
[JsonSerializable(typeof(SpiralAbyss))]
[JsonSerializable(typeof(RoleCombatData))]
[JsonSerializable(typeof(HardChallengeData))]
[JsonSerializable(typeof(uint))]
internal sealed partial class PersistenceJsonContext : JsonSerializerContext;
