// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Metadata.Tower;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using Snap.Hutao.Remastered.Web.Hoyolab;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.SpiralAbyss;

public sealed partial class SpiralAbyssView : ObservableObject, IEntityAccess<SpiralAbyssEntry?>, IPropertyValuesProvider
{
    private SpiralAbyssView(SpiralAbyssEntry entity, SpiralAbyssMetadataContext context)
        : this(entity, context.IdTowerScheduleMap[entity.ScheduleId], context)
    {
    }

    private SpiralAbyssView(SpiralAbyssEntry entity, TowerSchedule towerSchedule, SpiralAbyssMetadataContext context)
        : this(towerSchedule, context)
    {
        Entity = entity;

        Web.Hoyolab.Takumi.GameRecord.SpiralAbyss.SpiralAbyss spiralAbyss = entity.SpiralAbyss;
        TotalBattleTimes = spiralAbyss.TotalBattleTimes;
        TotalStar = spiralAbyss.TotalStar;
        MaxFloor = spiralAbyss.MaxFloor;
        Reveals = ToRankAvatars(spiralAbyss.RevealRank, context);
        Defeat = ToRankAvatar(spiralAbyss.DefeatRank, context);
        Damage = ToRankAvatar(spiralAbyss.DamageRank, context);
        TakeDamage = ToRankAvatar(spiralAbyss.TakeDamageRank, context);
        NormalSkill = ToRankAvatar(spiralAbyss.NormalSkillRank, context);
        EnergySkill = ToRankAvatar(spiralAbyss.EnergySkillRank, context);
        Engaged = true;

        TimeSpan offset = PlayerUid.GetRegionTimeZoneUtcOffsetForUid(entity.Uid);
        foreach (Web.Hoyolab.Takumi.GameRecord.SpiralAbyss.SpiralAbyssFloor webFloor in spiralAbyss.Floors)
        {
            // Ignoring floor 1 - 8 here
            if (Floors.Source.SingleOrDefault(f => f.IndexValue == webFloor.Index) is { } floor)
            {
                floor.Attach(webFloor, offset, context);
            }
        }
    }

    private SpiralAbyssView(TowerSchedule towerSchedule, SpiralAbyssMetadataContext context)
    {
        ScheduleId = towerSchedule.Id;
        FormattedTime = $"{towerSchedule.Open:yyyy.MM.dd HH:mm} - {towerSchedule.Close:yyyy.MM.dd HH:mm}";

        BlessingName = towerSchedule.BuffName;
        Blessings = towerSchedule.Descriptions;
        Floors = towerSchedule.FloorIds.Select(id => FloorView.Create(context.IdTowerFloorMap[id], context)).Reverse().AsAdvancedCollectionView();
    }

    public uint ScheduleId { get; private set => SetProperty(ref field, value); }

    public string Schedule { get => SH.FormatModelEntitySpiralAbyssSchedule(ScheduleId); }

    public string FormattedTime { get; private set => SetProperty(ref field, value); }

    public bool Engaged { get; private set => SetProperty(ref field, value); }

    public SpiralAbyssEntry? Entity { get; private set => SetProperty(ref field, value); }

    public string BlessingName { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<string> Blessings { get; private set => SetProperty(ref field, value); }

    public int TotalBattleTimes { get; private set => SetProperty(ref field, value); }

    public int TotalStar { get; private set => SetProperty(ref field, value); }

    public string MaxFloor { get; private set => SetProperty(ref field, value); } = default!;

    public ImmutableArray<RankAvatar> Reveals { get; private set => SetProperty(ref field, value); } = [];

    public IAdvancedCollectionView<FloorView> Floors { get; private set => SetProperty(ref field, value); }

    public RankAvatar? Defeat { get; private set => SetProperty(ref field, value); }

    public RankAvatar? Damage { get; private set => SetProperty(ref field, value); }

    public RankAvatar? TakeDamage { get; private set => SetProperty(ref field, value); }

    public RankAvatar? NormalSkill { get; private set => SetProperty(ref field, value); }

    public RankAvatar? EnergySkill { get; private set => SetProperty(ref field, value); }

    public static SpiralAbyssView From(SpiralAbyssEntry entity, SpiralAbyssMetadataContext context)
    {
        return new(entity, context);
    }

    public static SpiralAbyssView From(SpiralAbyssEntry? entity, TowerSchedule meta, SpiralAbyssMetadataContext context)
    {
        return entity is not null ? new(entity, meta, context) : new(meta, context);
    }

    private static ImmutableArray<RankAvatar> ToRankAvatars(ImmutableArray<Web.Hoyolab.Takumi.GameRecord.SpiralAbyss.SpiralAbyssRank> ranks, SpiralAbyssMetadataContext context)
    {
        return [.. ranks.Where(r => r.AvatarId != 0U).Select(r => new RankAvatar(r.Value, context.IdAvatarMap[r.AvatarId]))];
    }

    private static RankAvatar? ToRankAvatar(ImmutableArray<Web.Hoyolab.Takumi.GameRecord.SpiralAbyss.SpiralAbyssRank> ranks, SpiralAbyssMetadataContext context)
    {
        return ranks.Where(r => r.AvatarId != 0U).Select(r => new RankAvatar(r.Value, context.IdAvatarMap[r.AvatarId])).SingleOrDefault();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        switch (e.PropertyName)
        {
            case nameof(ScheduleId):
                OnPropertyChanged(nameof(Schedule));
                break;
        }
    }
}
