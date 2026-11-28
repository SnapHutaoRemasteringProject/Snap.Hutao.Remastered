// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Calculable;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Abstraction;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using Snap.Hutao.Remastered.ViewModel.Complex;
using Snap.Hutao.Remastered.ViewModel.GachaLog;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Weapon;

[ProtoContract]
public sealed partial class Weapon : ObservableObject, IDefaultIdentity<WeaponId>,
    INameQualityAccess,
    IStatisticsItemConvertible,
    ISummaryItemConvertible,
    IItemConvertible,
    ICalculableSource<ICalculableWeapon>,
    ICultivationItemsAccess,
    IPropertyValuesProvider
{
    [ProtoMember(1)]
    public WeaponId Id { get; init; }

    [ProtoMember(2)]
    public PromoteId PromoteId { get; init; }

    [ProtoMember(3)]
    public uint Sort { get; init; }

    [ProtoMember(4)]
    public WeaponType WeaponType { get; init; }

    [ProtoMember(5)]
    public QualityType RankLevel { get; init; }

    [ProtoMember(6)]
    public string Name { get; init; }

    [ProtoMember(7)]
    public string Description { get; init; }

    [ProtoMember(8)]
    public string Icon { get; init; }

    [ProtoMember(9)]
    public string AwakenIcon { get; init; }

    [ProtoMember(10)]
    public WeaponTypeValueCollection GrowCurves { get; init; }

    [ProtoMember(11)]
    public NameDescriptions? Affix { get; init; }

    [ProtoMember(12)]
    public ImmutableArray<MaterialId> CultivationItems { get; init; } = [];

    [JsonIgnore]
    [ObservableProperty]
    public partial WeaponCollocationView? CollocationView { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial List<Material>? CultivationItemsView { get; set; }

    [JsonIgnore]
    public QualityType Quality
    {
        get => RankLevel;
    }

    public uint MaxLevel { get => GetMaxLevelByQuality(Quality); }

    public static uint GetMaxLevelByQuality(QualityType quality)
    {
        return quality >= QualityType.QUALITY_BLUE ? 90U : 70U;
    }

    public ICalculableWeapon ToCalculable()
    {
        return CalculableWeapon.From(this);
    }

    public TItem ToItem<TItem>()
        where TItem : Model.Item, new()
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Icon = EquipIconConverter.IconNameToUri(Icon),
            Badge = WeaponTypeIconConverter.WeaponTypeToIconUri(WeaponType),
            Quality = RankLevel,
        };
    }

    public StatisticsItem ToStatisticsItem(int count)
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Icon = EquipIconConverter.IconNameToUri(Icon),
            Badge = WeaponTypeIconConverter.WeaponTypeToIconUri(WeaponType),
            Quality = RankLevel,
            Count = count,
        };
    }

    public SummaryItem ToSummaryItem(int lastPull, in DateTimeOffset time, bool isUp)
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Icon = EquipIconConverter.IconNameToUri(Icon),
            Badge = WeaponTypeIconConverter.WeaponTypeToIconUri(WeaponType),
            Time = time,
            Quality = RankLevel,
            LastPull = lastPull,
            IsUp = isUp,
        };
    }
}
