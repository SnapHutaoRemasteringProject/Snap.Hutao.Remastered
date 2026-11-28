// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.ExceptionService;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Wire;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto;

/// <summary>
/// Maps a <see cref="MetadataCategory"/> to the <see cref="LanguageMetadata"/> field that carries it.
/// </summary>
internal static class ProtoCategoryTable
{
    private static readonly ImmutableArray<IProtoCategoryAdapter> Adapters =
    [
        new ProtoCategoryAdapter<Model.Metadata.Achievement.Achievement>(MetadataCategories.Achievement, static language => language.Achievements),
        new ProtoCategoryAdapter<Model.Metadata.Achievement.AchievementGoal>(MetadataCategories.AchievementGoal, static language => language.AchievementGoals),
        new ProtoCategoryAdapter<Model.Metadata.Avatar.Avatar>(MetadataCategories.Avatar, static language => language.Avatars),
        new ProtoCategoryAdapter<GrowCurve>(MetadataCategories.AvatarCurve, static language => language.AvatarCurves),
        new ProtoCategoryAdapter<Promote>(MetadataCategories.AvatarPromote, static language => language.AvatarPromotes),
        new ProtoCategoryAdapter<Model.Metadata.Item.BydMaterial>(MetadataCategories.BydMaterial, static language => language.BydMaterials),
        new ProtoCategoryAdapter<Chapter>(MetadataCategories.Chapter, static language => language.Chapters),
        new ProtoCategoryAdapter<Combine>(MetadataCategories.Combine, static language => language.Combines),
        new ProtoCategoryAdapter<CookRecipe>(MetadataCategories.CookRecipe, static language => language.CookRecipes),
        new ProtoCategoryAdapter<Model.Metadata.Item.DisplayItem>(MetadataCategories.DisplayItem, static language => language.DisplayItems),
        new ProtoCategoryAdapter<GachaEvent>(MetadataCategories.GachaEvent, static language => language.GachaEvents),
        new ProtoCategoryAdapter<HardChallengeSchedule>(MetadataCategories.HardChallengeSchedule, static language => language.HardChallengeSchedules),
        new ProtoCategoryAdapter<HyperLinkName>(MetadataCategories.HyperLinkName, static language => language.HyperLinkNames),
        new ProtoCategoryAdapter<MainQuest>(MetadataCategories.MainQuest, static language => language.MainQuests),
        new ProtoCategoryAdapter<Model.Metadata.Item.Material>(MetadataCategories.Material, static language => language.Materials),
        new ProtoCategoryAdapter<Model.Metadata.Monster.Monster>(MetadataCategories.Monster, static language => language.Monsters),
        new ProtoCategoryAdapter<GrowCurve>(MetadataCategories.MonsterCurve, static language => language.MonsterCurves),
        new ProtoCategoryAdapter<Model.Metadata.Avatar.ProfilePicture>(MetadataCategories.ProfilePicture, static language => language.ProfilePictures),
        new ProtoCategoryAdapter<Model.Metadata.Reliquary.Reliquary>(MetadataCategories.Reliquary, static language => language.Reliquaries),
        new ProtoCategoryAdapter<Model.Metadata.Reliquary.ReliquaryMainAffix>(MetadataCategories.ReliquaryMainAffix, static language => language.ReliquaryMainAffixes),
        new ProtoCategoryAdapter<Model.Metadata.Reliquary.ReliquaryMainAffixLevel>(MetadataCategories.ReliquaryMainAffixLevel, static language => language.ReliquaryMainAffixLevels),
        new ProtoCategoryAdapter<Model.Metadata.Reliquary.ReliquarySet>(MetadataCategories.ReliquarySet, static language => language.ReliquarySets),
        new ProtoCategoryAdapter<Model.Metadata.Reliquary.ReliquarySubAffix>(MetadataCategories.ReliquarySubAffix, static language => language.ReliquarySubAffixes),
        new ProtoCategoryAdapter<RoleCombatSchedule>(MetadataCategories.RoleCombatSchedule, static language => language.RoleCombatSchedules),
        new ProtoCategoryAdapter<Model.Metadata.Tower.TowerFloor>(MetadataCategories.TowerFloor, static language => language.TowerFloors),
        new ProtoCategoryAdapter<Model.Metadata.Tower.TowerLevel>(MetadataCategories.TowerLevel, static language => language.TowerLevels),
        new ProtoCategoryAdapter<Model.Metadata.Tower.TowerSchedule>(MetadataCategories.TowerSchedule, static language => language.TowerSchedules),
        new ProtoCategoryAdapter<Model.Metadata.Weapon.Weapon>(MetadataCategories.Weapon, static language => language.Weapons),
        new ProtoCategoryAdapter<GrowCurve>(MetadataCategories.WeaponCurve, static language => language.WeaponCurves),
        new ProtoCategoryAdapter<Promote>(MetadataCategories.WeaponPromote, static language => language.WeaponPromotes),
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
    /// Every consumed category must be registered exactly once, and an explicitly ignored category
    /// must not be registered. Category fields that the bundle carries but this client ignores are
    /// skipped by protobuf-net as unknown fields.
    /// </summary>
    public static void EnsureCoverage()
    {
        if (Volatile.Read(ref coverageValidated) is 1)
        {
            return;
        }

        HashSet<string> mapped = [.. Adapters.Select(adapter => adapter.Category.ProtoName)];
        HashSet<string> expected = [.. MetadataCategories.All.Select(category => category.ProtoName)];
        HashSet<string> ignored = [.. MetadataCategories.IgnoredProtoCategories];

        List<string> missing = [.. expected.Where(name => !mapped.Contains(name))];
        List<string> unknown = [.. mapped.Where(name => !expected.Contains(name))];
        List<string> duplicates =
        [
            .. Adapters
                .GroupBy(adapter => adapter.Category.ProtoName, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key),
        ];
        List<string> conflicting = [.. mapped.Where(ignored.Contains)];

        if (missing.Count > 0 || unknown.Count > 0 || duplicates.Count > 0 || conflicting.Count > 0)
        {
            throw HutaoException.InvalidOperation(
                $"Proto metadata category coverage mismatch: missing=[{string.Join(',', missing)}], " +
                $"not in schema=[{string.Join(',', unknown)}], duplicates=[{string.Join(',', duplicates)}], " +
                $"also ignored=[{string.Join(',', conflicting)}]");
        }

        Volatile.Write(ref coverageValidated, 1);
    }
}
