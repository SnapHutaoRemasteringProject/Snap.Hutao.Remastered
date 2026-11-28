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
using Snap.Hutao.Remastered.ViewModel.Wiki;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[DebuggerDisplay("Name={Name},Id={Id}")]
[ProtoContract]
[WinRT.GeneratedBindableCustomProperty([nameof(Icon), nameof(Quality)], [])]
public partial class Avatar : ObservableObject, IDefaultIdentity<AvatarId>,
    INameQualityAccess,
    IStatisticsItemConvertible,
    ISummaryItemConvertible,
    IItemConvertible,
    ICalculableSource<ICalculableAvatar>,
    ICultivationItemsAccess,
    IPropertyValuesProvider,
    IJsonOnDeserialized
{
    [ProtoMember(1)]
    public AvatarId Id { get; init; }

    [ProtoMember(2)]
    public PromoteId PromoteId { get; init; }

    [ProtoMember(3)]
    public uint Sort { get; init; }

    [ProtoMember(4)]
    public BodyType Body { get; init; }

    [ProtoMember(5)]
    public string Icon { get; init; }

    [ProtoMember(6)]
    public string SideIcon { get; init; }

    [ProtoMember(7)]
    public string Name { get; init; }

    [ProtoMember(8)]
    public string Description { get; init; }

    [ProtoIgnore]
    public DateTimeOffset BeginTime { get; init; }

    [JsonIgnore]
    [ProtoMember(9)]
    public string BeginTimeWire
    {
        get => MetadataProtoTime.ToWire(BeginTime);
        init => BeginTime = MetadataProtoTime.FromWire(value);
    }

    [ProtoMember(10)]
    public QualityType Quality { get; init; }

    [ProtoMember(11)]
    public WeaponType Weapon { get; init; }

    [ProtoMember(12)]
    public AvatarBaseValue BaseValue { get; init; }

    [ProtoMember(13)]
    public TypeValueCollection<FightProperty, GrowCurveType> GrowCurves { get; init; }

    [ProtoMember(14)]
    public SkillDepot SkillDepot { get; init; }

    [ProtoMember(15)]
    public FetterInfo FetterInfo { get; init; }

    [ProtoMember(16)]
    public ImmutableArray<Costume> Costumes { get; init; } = [];

    [ProtoMember(17)]
    public ImmutableArray<MaterialId> CultivationItems { get; init; } = [];

    [ProtoMember(18)]
    public AvatarNameCard NameCard { get; init; }

    [ProtoMember(19)]
    public TraceEffect? TraceEffect { get; init; }

    [JsonIgnore]
    [ObservableProperty]
    public partial AvatarCollocationView? CollocationView { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial CookBonusView? CookBonusView { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CultivationItemSource))]
    public partial ImmutableArray<Material>? CultivationItemsView { get; set; }

    [JsonIgnore]
    public List<Material>? CultivationItemSource => CultivationItemsView is { } items ? items.IsDefaultOrEmpty ? [] : new(items) : null;

    [JsonIgnore]
    [ObservableProperty]
    public partial IAdvancedCollectionView<Costume>? CostumesView { get; set; }

    [SuppressMessage("", "CA1822")]
    public uint MaxLevel { get => GetMaxLevel(); }

    public static uint GetMaxLevel()
    {
        return 100U;
    }

    public ICalculableAvatar ToCalculable()
    {
        return CalculableAvatar.From(this);
    }

    public TItem ToItem<TItem>()
        where TItem : Model.Item, new()
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Icon = AvatarIconConverter.IconNameToUri(Icon),
            Badge = ElementNameIconConverter.ElementNameToUri(FetterInfo.VisionBefore),
            Quality = Quality,
        };
    }

    public StatisticsItem ToStatisticsItem(int count)
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Icon = AvatarIconConverter.IconNameToUri(Icon),
            Badge = ElementNameIconConverter.ElementNameToUri(FetterInfo.VisionBefore),
            Quality = Quality,

            Count = count,
        };
    }

    public SummaryItem ToSummaryItem(int lastPull, in DateTimeOffset time, bool isUp)
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Icon = AvatarIconConverter.IconNameToUri(Icon),
            Badge = ElementNameIconConverter.ElementNameToUri(FetterInfo.VisionBefore),
            Quality = Quality,

            Time = time,
            LastPull = lastPull,
            IsUp = isUp,
        };
    }

    [ProtoAfterDeserialization]
    public void OnDeserialized()
    {
        if (AvatarIds.UsesGnosis(Id))
        {
            FetterInfo.VisionOverride = SH.ViewPageWiKiAvatarGnosisTitle;
        }
    }
}
