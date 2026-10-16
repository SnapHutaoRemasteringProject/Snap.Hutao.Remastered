// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Model.Calculable;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.AvatarProperty;

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class AvatarView : ObservableObject, INameIconSide<Uri>,
    ICalculableSource<ICalculableAvatar>,
    IPropertyValuesProvider
{
    [ObservableProperty]
    public partial string Name { get; set; } = default!;

    [ObservableProperty]
    public partial Uri Icon { get; set; } = default!;

    [ObservableProperty]
    public partial Uri SideIcon { get; set; } = default!;

    [ObservableProperty]
    public partial Uri NameCard { get; set; } = default!;

    [ObservableProperty]
    public partial QualityType Quality { get; set; }

    [ObservableProperty]
    public partial ElementType Element { get; set; }

    public string Level { get => LevelFormat.Format(LevelNumber); }

    [ObservableProperty]
    public partial WeaponView? Weapon { get; set; }

    [ObservableProperty]
    public partial RecommendPropertiesView RecommendedProperties { get; set; } = default!;

    [ObservableProperty]
    public partial ImmutableArray<ReliquaryView> Reliquaries { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayScore))]
    [NotifyPropertyChangedFor(nameof(ScoreColorValue))]
    public partial double Score { get; set; }

    public string DisplayScore => string.Format(SH.ViewPageAvatarPropertyReliquaryScoreValue, Score);

    public int ScoreColorValue => (int)Math.Round(Score);

    /// <summary>
    /// 「我的角色」页面切换评分算法后就地重算所需的输入，来自 <see cref="Service.AvatarInfo.Factory.SummaryAvatarFactory"/>
    /// </summary>
    [ObservableProperty]
    public partial ImmutableArray<FightProperty> RecommendedSubProperties { get; set; } = [];

    /// <inheritdoc cref="RecommendedSubProperties"/>
    [ObservableProperty]
    public partial EnergyType EnergyType { get; set; }

    /// <inheritdoc cref="RecommendedSubProperties"/>
    [ObservableProperty]
    public partial bool IsCritEffective { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActivatedConstellationCount))]
    public partial ImmutableArray<ConstellationView> Constellations { get; set; } = [];

    public int ActivatedConstellationCount { get => Constellations.Count(c => c.IsActivated); }

    [ObservableProperty]
    public partial ImmutableArray<SkillView> Skills { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxHp))]
    [NotifyPropertyChangedFor(nameof(CurAttack))]
    [NotifyPropertyChangedFor(nameof(CurDefense))]
    [NotifyPropertyChangedFor(nameof(ElementMastery))]
    public partial ImmutableArray<AvatarProperty> Properties { get; set; } = [];

    [ObservableProperty]
    public partial uint FetterLevel { get; set; }

    [ObservableProperty]
    public partial ImmutableArray<bool> PromoteArray { get; set; } = [];

    [ObservableProperty]
    public partial AvatarId Id { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Level))]
    public partial uint LevelNumber { get; set; }

    public int MaxHp { get => GetAvatarPropertyValue(FightProperty.FIGHT_PROP_MAX_HP); }

    public int CurAttack { get => GetAvatarPropertyValue(FightProperty.FIGHT_PROP_CUR_ATTACK); }

    public int CurDefense { get => GetAvatarPropertyValue(FightProperty.FIGHT_PROP_CUR_DEFENSE); }

    public int ElementMastery { get => GetAvatarPropertyValue(FightProperty.FIGHT_PROP_ELEMENT_MASTERY); }

    [ObservableProperty]
    public partial global::Snap.Hutao.Remastered.Model.Primitive.PromoteLevel PromoteLevel { get; set; }

    public ICalculableAvatar ToCalculable()
    {
        return CalculableAvatar.From(this);
    }

    partial void OnConstellationsChanged(ImmutableArray<ConstellationView> oldValue, ImmutableArray<ConstellationView> newValue)
    {
        foreach (ConstellationView constellation in oldValue.EmptyIfDefault()) constellation.PropertyChanged -= OnConstellationPropertyChanged;
        foreach (ConstellationView constellation in newValue.EmptyIfDefault()) constellation.PropertyChanged += OnConstellationPropertyChanged;
    }

    private void OnConstellationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConstellationView.IsActivated)) OnPropertyChanged(nameof(ActivatedConstellationCount));
    }

    private int GetAvatarPropertyValue(FightProperty fightProperty)
    {
        AvatarProperty property = Properties.Single(p => p.FightProperty == fightProperty);
        int value = int.Parse(property.Value);
        if (!string.IsNullOrEmpty(property.AddValue))
        {
            value += int.Parse(property.AddValue);
        }

        return value;
    }
}
