// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Avatar;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;
using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using ProtoArkhe = Snap.Hutao.Remastered.Metadata.Arkhe;
using ProtoAssociationType = Snap.Hutao.Remastered.Metadata.AssociationType;
using ProtoAvatar = Snap.Hutao.Remastered.Metadata.Avatar;
using ProtoAvatarBaseValue = Snap.Hutao.Remastered.Metadata.AvatarBaseValue;
using ProtoAvatarCostume = Snap.Hutao.Remastered.Metadata.AvatarCostume;
using ProtoAvatarFetterInfo = Snap.Hutao.Remastered.Metadata.AvatarFetterInfo;
using ProtoAvatarNameCard = Snap.Hutao.Remastered.Metadata.AvatarNameCard;
using ProtoAvatarSkill = Snap.Hutao.Remastered.Metadata.AvatarSkill;
using ProtoAvatarSkillDepot = Snap.Hutao.Remastered.Metadata.AvatarSkillDepot;
using ProtoAvatarTalent = Snap.Hutao.Remastered.Metadata.AvatarTalent;
using ProtoAvatarTraceEffect = Snap.Hutao.Remastered.Metadata.AvatarTraceEffect;
using ProtoBodyType = Snap.Hutao.Remastered.Metadata.BodyType;
using ProtoEnergyType = Snap.Hutao.Remastered.Metadata.EnergyType;
using ProtoFetter = Snap.Hutao.Remastered.Metadata.Fetter;
using ProtoFetterStory = Snap.Hutao.Remastered.Metadata.FetterStory;
using ProtoQualityType = Snap.Hutao.Remastered.Metadata.QualityType;
using ProtoSkillProud = Snap.Hutao.Remastered.Metadata.SkillProud;
using ProtoWeaponType = Snap.Hutao.Remastered.Metadata.WeaponType;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class AvatarAdapter : ProtoCategoryAdapter<ProtoAvatar, Model.Metadata.Avatar.Avatar>
{
    public AvatarAdapter()
        : base(MetadataCategories.Avatar)
    {
    }

    public override IEnumerable<ProtoAvatar> Enumerate(LanguageMetadata language)
    {
        return language.Avatars;
    }

    public override Model.Metadata.Avatar.Avatar Map(ProtoAvatar message, MetadataConvert convert)
    {
        Avatar avatar = new()
        {
            Id = (AvatarId)message.Id,
            PromoteId = (PromoteId)message.PromoteId,
            Sort = message.Sort,
            Body = convert.ToEnum<ProtoBodyType, BodyType>(message.Body, nameof(message.Body)),
            Icon = convert.Text(message.Icon),
            SideIcon = convert.Text(message.SideIcon),
            Name = convert.Text(message.Name),
            Description = convert.Text(message.Description),
            BeginTime = convert.Time(message.BeginTime, nameof(message.BeginTime)),
            Quality = convert.ToEnum<ProtoQualityType, QualityType>(message.Quality, nameof(message.Quality)),
            Weapon = convert.ToEnum<ProtoWeaponType, WeaponType>(message.Weapon, nameof(message.Weapon)),
            BaseValue = MapBaseValue(message.BaseValue, convert),
            GrowCurves = convert.FightGrowCurveCollection(
                message.GrowCurves.Select(curve => (curve.Type, curve.Value)),
                nameof(message.GrowCurves)),
            SkillDepot = MapSkillDepot(message.SkillDepot, convert),
            FetterInfo = MapFetterInfo(message.FetterInfo, convert),
            Costumes = [.. message.Costumes.Select(costume => MapCostume(costume, convert))],
            CultivationItems = [.. message.CultivationItems.Select(id => (MaterialId)id)],
            NameCard = MapNameCard(message.NameCard, convert),
            TraceEffect = message.TraceEffect is { } traceEffect ? MapTraceEffect(traceEffect, convert) : null,
        };

        // Avatar.VisionOverride depends on IJsonOnDeserialized, which System.Text.Json calls for the
        // JSON source. The proto source must do the same or the two sources disagree.
        avatar.OnDeserialized();
        return avatar;
    }

    private static AvatarBaseValue MapBaseValue(ProtoAvatarBaseValue? value, MetadataConvert convert)
    {
        return value is null
            ? throw new MetadataEntrySkippedException(nameof(ProtoAvatar.BaseValue), "avatar has no base value")
            : new()
            {
                HpBase = convert.Single(value.HpBase),
                AttackBase = convert.Single(value.AttackBase),
                DefenseBase = convert.Single(value.DefenseBase),
            };
    }

    private static SkillDepot MapSkillDepot(ProtoAvatarSkillDepot? depot, MetadataConvert convert)
    {
        if (depot is null)
        {
            throw new MetadataEntrySkippedException(nameof(ProtoAvatar.SkillDepot), "avatar has no skill depot");
        }

        ImmutableArray<ProudSkill> specialSkills = [.. depot.SpecialSkills.Select(skill => MapProudSkill(skill, convert))];

        return new()
        {
            Arkhe = convert.ToEnum<ProtoArkhe, Arkhe>(depot.Arkhe, nameof(depot.Arkhe)),
            Skills = [.. depot.Skills.Select(skill => MapProudSkill(skill, convert))],
            SpecialSkills = specialSkills.IsEmpty ? null : specialSkills,
            EnergySkill = depot.EnergySkill is { } energySkill
                ? MapProudSkill(energySkill, convert)
                : throw new MetadataEntrySkippedException(nameof(ProtoAvatarSkillDepot.EnergySkill), "skill depot has no energy skill"),
            Inherents = [.. depot.Inherents.Select(skill => MapProudSkill(skill, convert))],
            Talents = [.. depot.Talents.Select(talent => MapTalent(talent, convert))],
        };
    }

    private static ProudSkill MapProudSkill(ProtoAvatarSkill skill, MetadataConvert convert)
    {
        float? cdTime = skill.CdTime > 0 ? skill.CdTime : null;

        return new()
        {
            GroupId = (SkillGroupId)skill.GroupId,
            Proud = skill.Proud is { } proud
                ? MapProud(proud, convert)
                : throw new MetadataEntrySkippedException(nameof(ProtoAvatarSkill.Proud), "skill has no proud parameters"),
            Id = (SkillId)skill.Id,
            Name = convert.Text(skill.Name),
            Description = convert.Text(skill.Description),
            SpecialDescription = convert.OptionalText(skill.SpecialDescription),
            SpecialEnergyType = convert.ToEnum<ProtoEnergyType, EnergyType>(skill.SpecialEnergyType, nameof(skill.SpecialEnergyType)),
            Icon = convert.Text(skill.Icon),
            CdTime = cdTime,
        };
    }

    private static DescriptionsParameters MapProud(ProtoSkillProud proud, MetadataConvert convert)
    {
        return new()
        {
            Descriptions = [.. proud.Descriptions.Select(convert.Text)],
            Parameters = new IdLevelParametersCollection<ProudSkillId, SkillLevel, float>(
            [
                .. proud.Parameters.Select(parameter => new IdLevelParameters<ProudSkillId, SkillLevel, float>(
                    (ProudSkillId)parameter.Id,
                    (SkillLevel)parameter.Level,
                    [.. parameter.Parameters.Select(convert.Single)])),
            ]),
            DescriptionList = proud.DescriptionList.Count is 0 ? null : proud.DescriptionList.ToArray(),
            SpecialDescriptionList = proud.SpecialDescriptionList.Count is 0 ? null : proud.SpecialDescriptionList.ToArray(),
        };
    }

    private static Skill MapTalent(ProtoAvatarTalent talent, MetadataConvert convert)
    {
        return new()
        {
            Id = (SkillId)talent.Id,
            Name = convert.Text(talent.Name),
            Description = convert.Text(talent.Description),
            SpecialDescription = convert.OptionalText(talent.SpecialDescription),
            Icon = convert.Text(talent.Icon),
            ExtraLevel = talent.ExtraLevel is { } extraLevel
                ? new ExtraLevelInfo
                {
                    Index = convert.RawEnum<ExtraLevelIndexKind>(extraLevel.Index, nameof(extraLevel.Index)),
                    Level = extraLevel.Level,
                }
                : null,
        };
    }

    private static FetterInfo MapFetterInfo(ProtoAvatarFetterInfo? info, MetadataConvert convert)
    {
        if (info is null)
        {
            throw new MetadataEntrySkippedException(nameof(ProtoAvatar.FetterInfo), "avatar has no fetter info");
        }

        return new()
        {
            Title = convert.Text(info.Title),
            Detail = convert.Text(info.Detail),
            Association = convert.ToEnum<ProtoAssociationType, AssociationType>(info.Association, nameof(info.Association)),
            Native = convert.Text(info.Native),
            BirthMonth = info.BirthMonth,
            BirthDay = info.BirthDay,
            VisionBefore = convert.Text(info.VisionBefore),
            VisionAfter = convert.OptionalText(info.VisionAfter),
            VisionOverrideLocked = convert.OptionalText(info.VisionOverrideLocked),
            VisionOverrideUnlocked = convert.OptionalText(info.VisionOverrideUnlocked),
            ConstellationBefore = convert.Text(info.ConstellationBefore),
            ConstellationAfter = convert.OptionalText(info.ConstellationAfter),
            CvChinese = convert.Text(info.CvChinese),
            CvJapanese = convert.Text(info.CvJapanese),
            CvEnglish = convert.Text(info.CvEnglish),
            CvKorean = convert.Text(info.CvKorean),
            CookBonus = info.CookBonus is { } cookBonus
                ? new()
                {
                    OriginItemId = (MaterialId)cookBonus.OriginItemId,
                    ItemId = (MaterialId)cookBonus.ItemId,
                    InputList = [.. cookBonus.InputList.Select(id => (MaterialId)id)],
                }
                : null,
            Fetters = [.. info.Fetters.Select(fetter => MapFetter(fetter, convert))],
            FetterStories = [.. info.FetterStories.Select(fetter => MapFetter(fetter, convert))],
        };
    }

    private static Fetter MapFetter(ProtoFetter fetter, MetadataConvert convert)
    {
        return new()
        {
            Title = convert.Text(fetter.Title),
            Context = convert.Text(fetter.Context),
        };
    }

    private static Fetter MapFetter(ProtoFetterStory fetter, MetadataConvert convert)
    {
        return new()
        {
            Title = convert.Text(fetter.Title),
            Context = convert.Text(fetter.Context),
        };
    }

    private static Costume MapCostume(ProtoAvatarCostume costume, MetadataConvert convert)
    {
        return new()
        {
            Id = (CostumeId)costume.Id,
            Name = convert.Text(costume.Name),
            Description = convert.Text(costume.Description),
            IsDefault = costume.IsDefault,
            FrontIcon = convert.OptionalText(costume.FrontIcon),
            SideIcon = convert.OptionalText(costume.SideIcon),
        };
    }

    private static Model.Metadata.Avatar.AvatarNameCard MapNameCard(ProtoAvatarNameCard? nameCard, MetadataConvert convert)
    {
        return nameCard is null
            ? throw new MetadataEntrySkippedException(nameof(ProtoAvatar.NameCard), "avatar has no name card")
            : new()
            {
                Name = convert.Text(nameCard.Name),
                Description = convert.Text(nameCard.Description),
                Icon = convert.Text(nameCard.Icon),
                PicturePrefix = convert.Text(nameCard.PicturePrefix),
            };
    }

    private static TraceEffect MapTraceEffect(ProtoAvatarTraceEffect traceEffect, MetadataConvert convert)
    {
        return new()
        {
            Id = (MaterialId)traceEffect.Id,
            Name = convert.Text(traceEffect.Name),
            Description = convert.Text(traceEffect.Description),
            Icon = convert.Text(traceEffect.Icon),
        };
    }
}
