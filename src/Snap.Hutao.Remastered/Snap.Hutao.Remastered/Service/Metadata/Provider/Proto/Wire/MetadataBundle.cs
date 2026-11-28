// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Metadata.Monster;
using Snap.Hutao.Remastered.Model.Metadata.Reliquary;
using Snap.Hutao.Remastered.Model.Metadata.Tower;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using System.Collections.Immutable;
using AchievementModel = Snap.Hutao.Remastered.Model.Metadata.Achievement.Achievement;
using AchievementGoalModel = Snap.Hutao.Remastered.Model.Metadata.Achievement.AchievementGoal;
using AvatarModel = Snap.Hutao.Remastered.Model.Metadata.Avatar.Avatar;
using WeaponModel = Snap.Hutao.Remastered.Model.Metadata.Weapon.Weapon;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Wire;

[ProtoContract]
public sealed class MetadataBundle
{
    [ProtoMember(1)]
    public GameVersion? Version { get; init; }

    [ProtoMember(2)]
    public MetadataInfo? Info { get; init; }

    [ProtoMember(3)]
    public ImmutableArray<LanguageMetadata> Languages { get; init; } = [];
}

[ProtoContract]
public sealed class GameVersion
{
    [ProtoMember(1)]
    public int Major { get; init; }

    [ProtoMember(2)]
    public int Minor { get; init; }

    [ProtoMember(3)]
    public int Patch { get; init; }
}

[ProtoContract]
public sealed class MetadataInfo
{
    [ProtoMember(1)]
    public string Publisher { get; init; } = string.Empty;

    [ProtoMember(2)]
    public string GeneratedAt { get; init; } = string.Empty;
}

[ProtoContract]
public sealed class LanguageMetadata
{
    [ProtoMember(1)]
    public string Language { get; init; } = string.Empty;

    [ProtoMember(2)]
    public ImmutableArray<AchievementModel> Achievements { get; init; } = [];

    [ProtoMember(3)]
    public ImmutableArray<AchievementGoalModel> AchievementGoals { get; init; } = [];

    [ProtoMember(4)]
    public ImmutableArray<AvatarModel> Avatars { get; init; } = [];

    [ProtoMember(5)]
    public ImmutableArray<GrowCurve> AvatarCurves { get; init; } = [];

    [ProtoMember(6)]
    public ImmutableArray<Promote> AvatarPromotes { get; init; } = [];

    [ProtoMember(7)]
    public ImmutableArray<BydMaterial> BydMaterials { get; init; } = [];

    [ProtoMember(8)]
    public ImmutableArray<Chapter> Chapters { get; init; } = [];

    [ProtoMember(9)]
    public ImmutableArray<Combine> Combines { get; init; } = [];

    [ProtoMember(10)]
    public ImmutableArray<CookRecipe> CookRecipes { get; init; } = [];

    [ProtoMember(11)]
    public ImmutableArray<DisplayItem> DisplayItems { get; init; } = [];

    [ProtoMember(16)]
    public ImmutableArray<GachaEvent> GachaEvents { get; init; } = [];

    [ProtoMember(17)]
    public ImmutableArray<HardChallengeSchedule> HardChallengeSchedules { get; init; } = [];

    [ProtoMember(18)]
    public ImmutableArray<HyperLinkName> HyperLinkNames { get; init; } = [];

    [ProtoMember(19)]
    public ImmutableArray<MainQuest> MainQuests { get; init; } = [];

    [ProtoMember(20)]
    public ImmutableArray<Material> Materials { get; init; } = [];

    [ProtoMember(21)]
    public ImmutableArray<Monster> Monsters { get; init; } = [];

    [ProtoMember(22)]
    public ImmutableArray<GrowCurve> MonsterCurves { get; init; } = [];

    [ProtoMember(23)]
    public ImmutableArray<ProfilePicture> ProfilePictures { get; init; } = [];

    [ProtoMember(24)]
    public ImmutableArray<Reliquary> Reliquaries { get; init; } = [];

    [ProtoMember(26)]
    public ImmutableArray<ReliquaryMainAffix> ReliquaryMainAffixes { get; init; } = [];

    [ProtoMember(27)]
    public ImmutableArray<ReliquaryMainAffixLevel> ReliquaryMainAffixLevels { get; init; } = [];

    [ProtoMember(28)]
    public ImmutableArray<ReliquarySet> ReliquarySets { get; init; } = [];

    [ProtoMember(29)]
    public ImmutableArray<ReliquarySubAffix> ReliquarySubAffixes { get; init; } = [];

    [ProtoMember(30)]
    public ImmutableArray<RoleCombatSchedule> RoleCombatSchedules { get; init; } = [];

    [ProtoMember(31)]
    public ImmutableArray<TowerFloor> TowerFloors { get; init; } = [];

    [ProtoMember(32)]
    public ImmutableArray<TowerLevel> TowerLevels { get; init; } = [];

    [ProtoMember(33)]
    public ImmutableArray<TowerSchedule> TowerSchedules { get; init; } = [];

    [ProtoMember(34)]
    public ImmutableArray<WeaponModel> Weapons { get; init; } = [];

    [ProtoMember(35)]
    public ImmutableArray<GrowCurve> WeaponCurves { get; init; } = [];

    [ProtoMember(36)]
    public ImmutableArray<Promote> WeaponPromotes { get; init; } = [];
}
