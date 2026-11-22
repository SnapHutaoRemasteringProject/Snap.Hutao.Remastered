// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Google.Protobuf.Reflection;
using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Maps a <see cref="MetadataCategory"/> to the proto category adapter that reads it, and asserts that
/// the registered adapters cover the whole schema.
/// </summary>
internal static class ProtoCategoryTable
{
    private static readonly ImmutableArray<IProtoCategoryAdapter> Adapters =
    [
        new AchievementAdapter(),
        new AchievementGoalAdapter(),
        new AvatarAdapter(),
        new AvatarCurveAdapter(),
        new AvatarPromoteAdapter(),
        new BydMaterialAdapter(),
        new ChapterAdapter(),
        new CombineAdapter(),
        new CookRecipeAdapter(),
        new DisplayItemAdapter(),
        new GachaEventAdapter(),
        new HardChallengeScheduleAdapter(),
        new HyperLinkNameAdapter(),
        new MainQuestAdapter(),
        new MaterialAdapter(),
        new MonsterAdapter(),
        new MonsterCurveAdapter(),
        new ProfilePictureAdapter(),
        new ReliquaryAdapter(),
        new ReliquaryMainAffixAdapter(),
        new ReliquaryMainAffixLevelAdapter(),
        new ReliquarySetAdapter(),
        new ReliquarySubAffixAdapter(),
        new RoleCombatScheduleAdapter(),
        new TowerFloorAdapter(),
        new TowerLevelAdapter(),
        new TowerScheduleAdapter(),
        new WeaponAdapter(),
        new WeaponCurveAdapter(),
        new WeaponPromoteAdapter(),
    ];

    private static readonly ImmutableDictionary<string, IProtoCategoryAdapter> ByProtoName =
        Adapters.ToImmutableDictionary(adapter => adapter.Category.ProtoName, StringComparer.Ordinal);

    private static int coverageValidated;

    public static int AdapterCount
    {
        get => Adapters.Length;
    }

    public static IProtoCategoryAdapter? GetAdapter(MetadataCategory category)
    {
        return ByProtoName.GetValueOrDefault(category.ProtoName);
    }

    /// <summary>
    /// Every <c>LanguageMetadata</c> message field must be either consumed by an adapter or listed in
    /// <see cref="MetadataCategories.IgnoredProtoCategories"/>. This turns a producer schema change
    /// into a loud, one-time failure instead of silently missing categories.
    /// </summary>
    public static void EnsureCoverage()
    {
        if (Volatile.Read(ref coverageValidated) is 1)
        {
            return;
        }

        HashSet<string> schemaNames =
        [
            .. LanguageMetadata.Descriptor.Fields
                .InDeclarationOrder()
                .Where(field => field.FieldType is FieldType.Message)
                .Select(field => field.MessageType.Name),
        ];

        HashSet<string> mapped = [.. Adapters.Select(adapter => adapter.Category.ProtoName)];
        HashSet<string> ignored = [.. MetadataCategories.IgnoredProtoCategories];

        List<string> missing = [.. schemaNames.Where(name => !mapped.Contains(name) && !ignored.Contains(name))];
        List<string> unknown = [.. mapped.Where(name => !schemaNames.Contains(name))];
        List<string> duplicates =
        [
            .. Adapters
                .GroupBy(adapter => adapter.Category.ProtoName, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key),
        ];

        if (missing.Count > 0 || unknown.Count > 0 || duplicates.Count > 0)
        {
            throw HutaoException.InvalidOperation(
                $"Proto metadata category coverage mismatch: missing=[{string.Join(',', missing)}], " +
                $"not in schema=[{string.Join(',', unknown)}], duplicates=[{string.Join(',', duplicates)}]");
        }

        Volatile.Write(ref coverageValidated, 1);
    }
}
