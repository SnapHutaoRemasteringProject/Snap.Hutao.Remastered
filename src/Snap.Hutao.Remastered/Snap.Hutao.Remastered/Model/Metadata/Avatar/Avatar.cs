// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

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
    public required AvatarId Id { get; init; }

    public required PromoteId PromoteId { get; init; }

    public required uint Sort { get; init; }

    public required BodyType Body { get; init; }

    public required string Icon { get; init; }

    public required string SideIcon { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required DateTimeOffset BeginTime { get; init; }

    public required QualityType Quality { get; init; }

    public required WeaponType Weapon { get; init; }

    public required AvatarBaseValue BaseValue { get; init; }

    public required TypeValueCollection<FightProperty, GrowCurveType> GrowCurves { get; init; }

    public required SkillDepot SkillDepot { get; init; }

    public required FetterInfo FetterInfo { get; init; }

    public required ImmutableArray<Costume> Costumes { get; init; }

    public required ImmutableArray<MaterialId> CultivationItems { get; init; }

    public required AvatarNameCard NameCard { get; init; }

    public TraceEffect? TraceEffect { get; init; }

    [JsonIgnore]
    [ObservableProperty]
    public partial AvatarCollocationView? CollocationView { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial CookBonusView? CookBonusView { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial ImmutableArray<Material>? CultivationItemsView { get; set; }

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

    public void OnDeserialized()
    {
        if (AvatarIds.UsesGnosis(Id))
        {
            FetterInfo.VisionOverride = SH.ViewPageWiKiAvatarGnosisTitle;
        }
    }
}
