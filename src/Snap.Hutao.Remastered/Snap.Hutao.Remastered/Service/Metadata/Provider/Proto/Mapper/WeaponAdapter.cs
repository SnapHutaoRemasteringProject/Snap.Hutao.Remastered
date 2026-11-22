// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Model.Primitive;
using QualityType = Snap.Hutao.Remastered.Model.Intrinsic.QualityType;
using WeaponType = Snap.Hutao.Remastered.Model.Intrinsic.WeaponType;
using ProtoQualityType = Snap.Hutao.Remastered.Metadata.QualityType;
using ProtoWeapon = Snap.Hutao.Remastered.Metadata.Weapon;
using ProtoWeaponType = Snap.Hutao.Remastered.Metadata.WeaponType;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class WeaponAdapter : ProtoCategoryAdapter<ProtoWeapon, Model.Metadata.Weapon.Weapon>
{
    public WeaponAdapter()
        : base(MetadataCategories.Weapon)
    {
    }

    public override IEnumerable<ProtoWeapon> Enumerate(LanguageMetadata language)
    {
        return language.Weapons;
    }

    public override Model.Metadata.Weapon.Weapon Map(ProtoWeapon message, MetadataConvert convert)
    {
        return new()
        {
            Id = (WeaponId)message.Id,
            PromoteId = (PromoteId)message.PromoteId,
            Sort = message.Sort,
            WeaponType = convert.ToEnum<ProtoWeaponType, WeaponType>(message.WeaponType, nameof(message.WeaponType)),
            RankLevel = convert.ToEnum<ProtoQualityType, QualityType>(message.RankLevel, nameof(message.RankLevel)),
            Name = convert.Text(message.Name),
            Description = convert.Text(message.Description),
            Icon = convert.Text(message.Icon),
            AwakenIcon = convert.Text(message.AwakenIcon),
            GrowCurves = convert.WeaponGrowCurveCollection(message.GrowCurves, nameof(message.GrowCurves)),
            Affix = message.Affix is { } affix
                ? new()
                {
                    Name = convert.Text(affix.Name),
                    Descriptions =
                    [
                        .. affix.Descriptions.Select(description => new LevelDescription
                        {
                            Level = description.Level,
                            Description = convert.Text(description.Description),
                        }),
                    ],
                }
                : null,
            CultivationItems = [.. message.CultivationItems.Select(id => (MaterialId)id)],
        };
    }
}
