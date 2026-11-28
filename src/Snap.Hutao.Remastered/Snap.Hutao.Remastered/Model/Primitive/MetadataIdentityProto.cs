// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using ProtoBuf.Serializers;

namespace Snap.Hutao.Remastered.Model.Primitive;

// The identity generator owns the uint storage and the implicit operators. These declarations add
// the protobuf-net scalar serializer: an identity is one uint32 varint on the wire, not a message.
[ProtoContract(Serializer = typeof(AchievementGoalIdSerializer), IsScalar = true)]
public readonly partial struct AchievementGoalId;

[ProtoContract(Serializer = typeof(AchievementIdSerializer), IsScalar = true)]
public readonly partial struct AchievementId;

[ProtoContract(Serializer = typeof(AvatarIdSerializer), IsScalar = true)]
public readonly partial struct AvatarId;

[ProtoContract(Serializer = typeof(BydMaterialIdSerializer), IsScalar = true)]
public readonly partial struct BydMaterialId;

[ProtoContract(Serializer = typeof(ChapterIdSerializer), IsScalar = true)]
public readonly partial struct ChapterId;

[ProtoContract(Serializer = typeof(ChapterGroupIdSerializer), IsScalar = true)]
public readonly partial struct ChapterGroupId;

[ProtoContract(Serializer = typeof(CombineIdSerializer), IsScalar = true)]
public readonly partial struct CombineId;

[ProtoContract(Serializer = typeof(CookRecipeIdSerializer), IsScalar = true)]
public readonly partial struct CookRecipeId;

[ProtoContract(Serializer = typeof(CostumeIdSerializer), IsScalar = true)]
public readonly partial struct CostumeId;

[ProtoContract(Serializer = typeof(EquipAffixIdSerializer), IsScalar = true)]
public readonly partial struct EquipAffixId;

[ProtoContract(Serializer = typeof(ExtendedEquipAffixIdSerializer), IsScalar = true)]
public readonly partial struct ExtendedEquipAffixId;

[ProtoContract(Serializer = typeof(FetterLevelSerializer), IsScalar = true)]
public readonly partial struct FetterLevel;

[ProtoContract(Serializer = typeof(FurnitureIdSerializer), IsScalar = true)]
public readonly partial struct FurnitureId;

[ProtoContract(Serializer = typeof(FurnitureMakeIdSerializer), IsScalar = true)]
public readonly partial struct FurnitureMakeId;

[ProtoContract(Serializer = typeof(FurnitureSuiteIdSerializer), IsScalar = true)]
public readonly partial struct FurnitureSuiteId;

[ProtoContract(Serializer = typeof(FurnitureTypeIdSerializer), IsScalar = true)]
public readonly partial struct FurnitureTypeId;

[ProtoContract(Serializer = typeof(HardChallengeScheduleIdSerializer), IsScalar = true)]
public readonly partial struct HardChallengeScheduleId;

[ProtoContract(Serializer = typeof(HyperLinkNameIdSerializer), IsScalar = true)]
public readonly partial struct HyperLinkNameId;

[ProtoContract(Serializer = typeof(LevelSerializer), IsScalar = true)]
public readonly partial struct Level;

[ProtoContract(Serializer = typeof(MainQuestIdSerializer), IsScalar = true)]
public readonly partial struct MainQuestId;

[ProtoContract(Serializer = typeof(MaterialIdSerializer), IsScalar = true)]
public readonly partial struct MaterialId;

[ProtoContract(Serializer = typeof(MonsterDescribeIdSerializer), IsScalar = true)]
public readonly partial struct MonsterDescribeId;

[ProtoContract(Serializer = typeof(MonsterIdSerializer), IsScalar = true)]
public readonly partial struct MonsterId;

[ProtoContract(Serializer = typeof(NameCardIdSerializer), IsScalar = true)]
public readonly partial struct NameCardId;

[ProtoContract(Serializer = typeof(ProfilePictureIdSerializer), IsScalar = true)]
public readonly partial struct ProfilePictureId;

[ProtoContract(Serializer = typeof(PromoteIdSerializer), IsScalar = true)]
public readonly partial struct PromoteId;

[ProtoContract(Serializer = typeof(PromoteLevelSerializer), IsScalar = true)]
public readonly partial struct PromoteLevel;

[ProtoContract(Serializer = typeof(ProudSkillIdSerializer), IsScalar = true)]
public readonly partial struct ProudSkillId;

[ProtoContract(Serializer = typeof(QuestIdSerializer), IsScalar = true)]
public readonly partial struct QuestId;

[ProtoContract(Serializer = typeof(ReliquaryLevelSerializer), IsScalar = true)]
public readonly partial struct ReliquaryLevel;

[ProtoContract(Serializer = typeof(ReliquarySubAffixIdSerializer), IsScalar = true)]
public readonly partial struct ReliquarySubAffixId;

[ProtoContract(Serializer = typeof(ReliquaryIdSerializer), IsScalar = true)]
public readonly partial struct ReliquaryId;

[ProtoContract(Serializer = typeof(ReliquaryMainAffixIdSerializer), IsScalar = true)]
public readonly partial struct ReliquaryMainAffixId;

[ProtoContract(Serializer = typeof(ReliquarySetIdSerializer), IsScalar = true)]
public readonly partial struct ReliquarySetId;

[ProtoContract(Serializer = typeof(RoleCombatScheduleIdSerializer), IsScalar = true)]
public readonly partial struct RoleCombatScheduleId;

[ProtoContract(Serializer = typeof(SkillGroupIdSerializer), IsScalar = true)]
public readonly partial struct SkillGroupId;

[ProtoContract(Serializer = typeof(SkillIdSerializer), IsScalar = true)]
public readonly partial struct SkillId;

[ProtoContract(Serializer = typeof(SkillLevelSerializer), IsScalar = true)]
public readonly partial struct SkillLevel;

[ProtoContract(Serializer = typeof(TowerLevelIdSerializer), IsScalar = true)]
public readonly partial struct TowerLevelId;

[ProtoContract(Serializer = typeof(TowerLevelGroupIdSerializer), IsScalar = true)]
public readonly partial struct TowerLevelGroupId;

[ProtoContract(Serializer = typeof(TowerFloorIdSerializer), IsScalar = true)]
public readonly partial struct TowerFloorId;

[ProtoContract(Serializer = typeof(TowerScheduleIdSerializer), IsScalar = true)]
public readonly partial struct TowerScheduleId;

[ProtoContract(Serializer = typeof(WeaponAffixLevelSerializer), IsScalar = true)]
public readonly partial struct WeaponAffixLevel;

[ProtoContract(Serializer = typeof(WeaponIdSerializer), IsScalar = true)]
public readonly partial struct WeaponId;

internal abstract class MetadataIdentitySerializer<TIdentity> : ISerializer<TIdentity>
    where TIdentity : struct
{
    public SerializerFeatures Features
    {
        get => SerializerFeatures.WireTypeVarint | SerializerFeatures.CategoryScalar;
    }

    public TIdentity Read(ref ProtoReader.State state, TIdentity value)
    {
        return FromUInt32(state.ReadUInt32());
    }

    public void Write(ref ProtoWriter.State state, TIdentity value)
    {
        state.WriteUInt32(ToUInt32(value));
    }

    protected abstract uint ToUInt32(TIdentity value);

    protected abstract TIdentity FromUInt32(uint value);
}

internal sealed class AchievementGoalIdSerializer : MetadataIdentitySerializer<AchievementGoalId>
{
    protected override uint ToUInt32(AchievementGoalId value) => value;

    protected override AchievementGoalId FromUInt32(uint value) => value;
}

internal sealed class AchievementIdSerializer : MetadataIdentitySerializer<AchievementId>
{
    protected override uint ToUInt32(AchievementId value) => value;

    protected override AchievementId FromUInt32(uint value) => value;
}

internal sealed class AvatarIdSerializer : MetadataIdentitySerializer<AvatarId>
{
    protected override uint ToUInt32(AvatarId value) => value;

    protected override AvatarId FromUInt32(uint value) => value;
}

internal sealed class BydMaterialIdSerializer : MetadataIdentitySerializer<BydMaterialId>
{
    protected override uint ToUInt32(BydMaterialId value) => value;

    protected override BydMaterialId FromUInt32(uint value) => value;
}

internal sealed class ChapterIdSerializer : MetadataIdentitySerializer<ChapterId>
{
    protected override uint ToUInt32(ChapterId value) => value;

    protected override ChapterId FromUInt32(uint value) => value;
}

internal sealed class ChapterGroupIdSerializer : MetadataIdentitySerializer<ChapterGroupId>
{
    protected override uint ToUInt32(ChapterGroupId value) => value;

    protected override ChapterGroupId FromUInt32(uint value) => value;
}

internal sealed class CombineIdSerializer : MetadataIdentitySerializer<CombineId>
{
    protected override uint ToUInt32(CombineId value) => value;

    protected override CombineId FromUInt32(uint value) => value;
}

internal sealed class CookRecipeIdSerializer : MetadataIdentitySerializer<CookRecipeId>
{
    protected override uint ToUInt32(CookRecipeId value) => value;

    protected override CookRecipeId FromUInt32(uint value) => value;
}

internal sealed class CostumeIdSerializer : MetadataIdentitySerializer<CostumeId>
{
    protected override uint ToUInt32(CostumeId value) => value;

    protected override CostumeId FromUInt32(uint value) => value;
}

internal sealed class EquipAffixIdSerializer : MetadataIdentitySerializer<EquipAffixId>
{
    protected override uint ToUInt32(EquipAffixId value) => value;

    protected override EquipAffixId FromUInt32(uint value) => value;
}

internal sealed class ExtendedEquipAffixIdSerializer : MetadataIdentitySerializer<ExtendedEquipAffixId>
{
    protected override uint ToUInt32(ExtendedEquipAffixId value) => value;

    protected override ExtendedEquipAffixId FromUInt32(uint value) => value;
}

internal sealed class FetterLevelSerializer : MetadataIdentitySerializer<FetterLevel>
{
    protected override uint ToUInt32(FetterLevel value) => value;

    protected override FetterLevel FromUInt32(uint value) => value;
}

internal sealed class FurnitureIdSerializer : MetadataIdentitySerializer<FurnitureId>
{
    protected override uint ToUInt32(FurnitureId value) => value;

    protected override FurnitureId FromUInt32(uint value) => value;
}

internal sealed class FurnitureMakeIdSerializer : MetadataIdentitySerializer<FurnitureMakeId>
{
    protected override uint ToUInt32(FurnitureMakeId value) => value;

    protected override FurnitureMakeId FromUInt32(uint value) => value;
}

internal sealed class FurnitureSuiteIdSerializer : MetadataIdentitySerializer<FurnitureSuiteId>
{
    protected override uint ToUInt32(FurnitureSuiteId value) => value;

    protected override FurnitureSuiteId FromUInt32(uint value) => value;
}

internal sealed class FurnitureTypeIdSerializer : MetadataIdentitySerializer<FurnitureTypeId>
{
    protected override uint ToUInt32(FurnitureTypeId value) => value;

    protected override FurnitureTypeId FromUInt32(uint value) => value;
}

internal sealed class HardChallengeScheduleIdSerializer : MetadataIdentitySerializer<HardChallengeScheduleId>
{
    protected override uint ToUInt32(HardChallengeScheduleId value) => value;

    protected override HardChallengeScheduleId FromUInt32(uint value) => value;
}

internal sealed class HyperLinkNameIdSerializer : MetadataIdentitySerializer<HyperLinkNameId>
{
    protected override uint ToUInt32(HyperLinkNameId value) => value;

    protected override HyperLinkNameId FromUInt32(uint value) => value;
}

internal sealed class LevelSerializer : MetadataIdentitySerializer<Level>
{
    protected override uint ToUInt32(Level value) => value;

    protected override Level FromUInt32(uint value) => value;
}

internal sealed class MainQuestIdSerializer : MetadataIdentitySerializer<MainQuestId>
{
    protected override uint ToUInt32(MainQuestId value) => value;

    protected override MainQuestId FromUInt32(uint value) => value;
}

internal sealed class MaterialIdSerializer : MetadataIdentitySerializer<MaterialId>
{
    protected override uint ToUInt32(MaterialId value) => value;

    protected override MaterialId FromUInt32(uint value) => value;
}

internal sealed class MonsterDescribeIdSerializer : MetadataIdentitySerializer<MonsterDescribeId>
{
    protected override uint ToUInt32(MonsterDescribeId value) => value;

    protected override MonsterDescribeId FromUInt32(uint value) => value;
}

internal sealed class MonsterIdSerializer : MetadataIdentitySerializer<MonsterId>
{
    protected override uint ToUInt32(MonsterId value) => value;

    protected override MonsterId FromUInt32(uint value) => value;
}

internal sealed class NameCardIdSerializer : MetadataIdentitySerializer<NameCardId>
{
    protected override uint ToUInt32(NameCardId value) => value;

    protected override NameCardId FromUInt32(uint value) => value;
}

internal sealed class ProfilePictureIdSerializer : MetadataIdentitySerializer<ProfilePictureId>
{
    protected override uint ToUInt32(ProfilePictureId value) => value;

    protected override ProfilePictureId FromUInt32(uint value) => value;
}

internal sealed class PromoteIdSerializer : MetadataIdentitySerializer<PromoteId>
{
    protected override uint ToUInt32(PromoteId value) => value;

    protected override PromoteId FromUInt32(uint value) => value;
}

internal sealed class PromoteLevelSerializer : MetadataIdentitySerializer<PromoteLevel>
{
    protected override uint ToUInt32(PromoteLevel value) => value;

    protected override PromoteLevel FromUInt32(uint value) => value;
}

internal sealed class ProudSkillIdSerializer : MetadataIdentitySerializer<ProudSkillId>
{
    protected override uint ToUInt32(ProudSkillId value) => value;

    protected override ProudSkillId FromUInt32(uint value) => value;
}

internal sealed class QuestIdSerializer : MetadataIdentitySerializer<QuestId>
{
    protected override uint ToUInt32(QuestId value) => value;

    protected override QuestId FromUInt32(uint value) => value;
}

internal sealed class ReliquaryLevelSerializer : MetadataIdentitySerializer<ReliquaryLevel>
{
    protected override uint ToUInt32(ReliquaryLevel value) => value;

    protected override ReliquaryLevel FromUInt32(uint value) => value;
}

internal sealed class ReliquarySubAffixIdSerializer : MetadataIdentitySerializer<ReliquarySubAffixId>
{
    protected override uint ToUInt32(ReliquarySubAffixId value) => value;

    protected override ReliquarySubAffixId FromUInt32(uint value) => value;
}

internal sealed class ReliquaryIdSerializer : MetadataIdentitySerializer<ReliquaryId>
{
    protected override uint ToUInt32(ReliquaryId value) => value;

    protected override ReliquaryId FromUInt32(uint value) => value;
}

internal sealed class ReliquaryMainAffixIdSerializer : MetadataIdentitySerializer<ReliquaryMainAffixId>
{
    protected override uint ToUInt32(ReliquaryMainAffixId value) => value;

    protected override ReliquaryMainAffixId FromUInt32(uint value) => value;
}

internal sealed class ReliquarySetIdSerializer : MetadataIdentitySerializer<ReliquarySetId>
{
    protected override uint ToUInt32(ReliquarySetId value) => value;

    protected override ReliquarySetId FromUInt32(uint value) => value;
}

internal sealed class RoleCombatScheduleIdSerializer : MetadataIdentitySerializer<RoleCombatScheduleId>
{
    protected override uint ToUInt32(RoleCombatScheduleId value) => value;

    protected override RoleCombatScheduleId FromUInt32(uint value) => value;
}

internal sealed class SkillGroupIdSerializer : MetadataIdentitySerializer<SkillGroupId>
{
    protected override uint ToUInt32(SkillGroupId value) => value;

    protected override SkillGroupId FromUInt32(uint value) => value;
}

internal sealed class SkillIdSerializer : MetadataIdentitySerializer<SkillId>
{
    protected override uint ToUInt32(SkillId value) => value;

    protected override SkillId FromUInt32(uint value) => value;
}

internal sealed class SkillLevelSerializer : MetadataIdentitySerializer<SkillLevel>
{
    protected override uint ToUInt32(SkillLevel value) => value;

    protected override SkillLevel FromUInt32(uint value) => value;
}

internal sealed class TowerLevelIdSerializer : MetadataIdentitySerializer<TowerLevelId>
{
    protected override uint ToUInt32(TowerLevelId value) => value;

    protected override TowerLevelId FromUInt32(uint value) => value;
}

internal sealed class TowerLevelGroupIdSerializer : MetadataIdentitySerializer<TowerLevelGroupId>
{
    protected override uint ToUInt32(TowerLevelGroupId value) => value;

    protected override TowerLevelGroupId FromUInt32(uint value) => value;
}

internal sealed class TowerFloorIdSerializer : MetadataIdentitySerializer<TowerFloorId>
{
    protected override uint ToUInt32(TowerFloorId value) => value;

    protected override TowerFloorId FromUInt32(uint value) => value;
}

internal sealed class TowerScheduleIdSerializer : MetadataIdentitySerializer<TowerScheduleId>
{
    protected override uint ToUInt32(TowerScheduleId value) => value;

    protected override TowerScheduleId FromUInt32(uint value) => value;
}

internal sealed class WeaponAffixLevelSerializer : MetadataIdentitySerializer<WeaponAffixLevel>
{
    protected override uint ToUInt32(WeaponAffixLevel value) => value;

    protected override WeaponAffixLevel FromUInt32(uint value) => value;
}

internal sealed class WeaponIdSerializer : MetadataIdentitySerializer<WeaponId>
{
    protected override uint ToUInt32(WeaponId value) => value;

    protected override WeaponId FromUInt32(uint value) => value;
}
