// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Model.Primitive.Converter;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;

namespace Snap.Hutao.Remastered.Core.Text.Json;

internal static class MetadataCollectionJson
{
    private static readonly ConditionalWeakTable<JsonSerializerOptions, MetadataCollectionJsonContext> Contexts = new();

    public static T? Deserialize<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        MetadataCollectionJsonContext context = Contexts.GetValue(options, static source =>
        {
            JsonSerializerOptions copy = new(source) { TypeInfoResolver = null };
            copy.Converters.Add(new IdentityConverter<ProudSkillId>());
            copy.Converters.Add(new IdentityConverter<SkillLevel>());
            return new(copy);
        });
        return context.GetTypeInfo(typeof(T)) is JsonTypeInfo<T> info
            ? JsonSerializer.Deserialize(ref reader, info)
            : throw new NotSupportedException("Register the metadata collection type in MetadataCollectionJsonContext.");
    }
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ImmutableArray<TypeValue<FightProperty, float>>))]
[JsonSerializable(typeof(ImmutableArray<TypeValue<FightProperty, GrowCurveType>>))]
[JsonSerializable(typeof(ImmutableArray<TypeValue<GrowCurveType, float>>))]
[JsonSerializable(typeof(ImmutableArray<IdLevelParameters<ProudSkillId, SkillLevel, float>>))]
[JsonSerializable(typeof(ImmutableArray<WeaponTypeValue>))]
[JsonSerializable(typeof(uint))]
internal sealed partial class MetadataCollectionJsonContext : JsonSerializerContext;
