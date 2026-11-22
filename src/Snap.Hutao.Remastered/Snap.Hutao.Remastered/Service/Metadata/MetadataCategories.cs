// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata;

public static class MetadataCategories
{
    public static readonly MetadataCategory Achievement = new("Achievement");
    public static readonly MetadataCategory AchievementGoal = new("AchievementGoal");
    public static readonly MetadataCategory Avatar = new("Avatar", isScattered: true);
    public static readonly MetadataCategory AvatarCurve = new("AvatarCurve");
    public static readonly MetadataCategory AvatarPromote = new("AvatarPromote");

    // "BeyondItem" is the JSON file name still published by Snap.Metadata; the proto category has
    // been renamed to BydMaterial (MetadataBuilder.ProtoBuilder.LegacyJsonFileNames keeps the pair).
    public static readonly MetadataCategory BydMaterial = new("BeyondItem", "BydMaterial");

    public static readonly MetadataCategory Chapter = new("Chapter");
    public static readonly MetadataCategory Combine = new("Combine");
    public static readonly MetadataCategory CookRecipe = new("CookRecipe");
    public static readonly MetadataCategory DisplayItem = new("DisplayItem");
    public static readonly MetadataCategory GachaEvent = new("GachaEvent");
    public static readonly MetadataCategory HardChallengeSchedule = new("HardChallengeSchedule");
    public static readonly MetadataCategory HyperLinkName = new("HyperLinkName");
    public static readonly MetadataCategory MainQuest = new("MainQuest");
    public static readonly MetadataCategory Material = new("Material");
    public static readonly MetadataCategory Monster = new("Monster");
    public static readonly MetadataCategory MonsterCurve = new("MonsterCurve");
    public static readonly MetadataCategory ProfilePicture = new("ProfilePicture");
    public static readonly MetadataCategory Reliquary = new("Reliquary");
    public static readonly MetadataCategory ReliquaryMainAffix = new("ReliquaryMainAffix");
    public static readonly MetadataCategory ReliquaryMainAffixLevel = new("ReliquaryMainAffixLevel");
    public static readonly MetadataCategory ReliquarySet = new("ReliquarySet");
    public static readonly MetadataCategory ReliquarySubAffix = new("ReliquarySubAffix");
    public static readonly MetadataCategory RoleCombatSchedule = new("RoleCombatSchedule");
    public static readonly MetadataCategory TowerFloor = new("TowerFloor");
    public static readonly MetadataCategory TowerLevel = new("TowerLevel");
    public static readonly MetadataCategory TowerSchedule = new("TowerSchedule");
    public static readonly MetadataCategory Weapon = new("Weapon");
    public static readonly MetadataCategory WeaponCurve = new("WeaponCurve");
    public static readonly MetadataCategory WeaponPromote = new("WeaponPromote");

    /// <summary>
    /// Every category the application consumes, in the order used for diagnostics.
    /// </summary>
    public static readonly ImmutableArray<MetadataCategory> All =
    [
        Achievement,
        AchievementGoal,
        Avatar,
        AvatarCurve,
        AvatarPromote,
        BydMaterial,
        Chapter,
        Combine,
        CookRecipe,
        DisplayItem,
        GachaEvent,
        HardChallengeSchedule,
        HyperLinkName,
        MainQuest,
        Material,
        Monster,
        MonsterCurve,
        ProfilePicture,
        Reliquary,
        ReliquaryMainAffix,
        ReliquaryMainAffixLevel,
        ReliquarySet,
        ReliquarySubAffix,
        RoleCombatSchedule,
        TowerFloor,
        TowerLevel,
        TowerSchedule,
        Weapon,
        WeaponCurve,
        WeaponPromote,
    ];

    /// <summary>
    /// Proto categories that exist in the schema but are not consumed by this version. They are
    /// declared explicitly so the coverage check can tell "intentionally ignored" apart from
    /// "forgot to register an adapter".
    /// </summary>
    public static readonly ImmutableArray<string> IgnoredProtoCategories =
    [
        "Furniture",
        "FurnitureMake",
        "FurnitureSuite",
        "FurnitureType",
        "ReliquaryAffixWeight",
    ];
}
