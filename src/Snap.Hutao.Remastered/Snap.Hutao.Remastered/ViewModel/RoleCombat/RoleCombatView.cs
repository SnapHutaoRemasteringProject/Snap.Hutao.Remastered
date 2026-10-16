// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using MetadataRoleCombatSchedule = Snap.Hutao.Remastered.Model.Metadata.RoleCombatSchedule;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.UI.Xaml.Data;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;
using Snap.Hutao.Remastered.Web.Hoyolab;
using System.Collections.Immutable;
using System.Globalization;

namespace Snap.Hutao.Remastered.ViewModel.RoleCombat;

public sealed partial class RoleCombatView : ObservableObject, IEntityAccess<RoleCombatEntry?>, IPropertyValuesProvider
{
    private RoleCombatView(RoleCombatEntry entity, RoleCombatMetadataContext context)
        : this(entity, context.IdRoleCombatScheduleMap[entity.ScheduleId], context)
    {
    }

    private RoleCombatView(RoleCombatEntry entity, MetadataRoleCombatSchedule roleCombatSchedule, RoleCombatMetadataContext context)
        : this(roleCombatSchedule, context)
    {
        Entity = entity;

        RoleCombatData roleCombatData = entity.RoleCombatData;
        Stat = roleCombatData.Stat;
        Difficulty = roleCombatData.Stat.DifficultyId.GetLocalizedDescription(SH.ResourceManager, CultureInfo.CurrentCulture);
        FormattedHeraldry = SH.FormatViewModelRoleCombatHeraldry(MaxRound);
        BackupAvatars =
        [
            .. roleCombatData.Detail.BackupAvatars
                .OrderByDescending(static avatar => avatar.AvatarType)
                .ThenByDescending(static avatar => avatar.Rarity)
                .ThenByDescending(static avatar => avatar.Element)
                .ThenByDescending(static avatar => avatar.AvatarId.Value)
                .Select(avatar => AvatarView.Create(avatar, context))
        ];
        TimeSpan offset = PlayerUid.GetRegionTimeZoneUtcOffsetForUid(entity.Uid);
        Rounds = roleCombatData.Detail.RoundsData.SelectAsArray(static (data, state) => RoundView.Create(data, state.offset, state.context), (offset, context));
        TotalBattleTimes = roleCombatData.Detail.FightStatistics.TotalUseTime;

        if (roleCombatData.Detail.FightStatistics.IsShowBattleStats)
        {
            Shortest = ToAvatarDamages(roleCombatData.Detail.FightStatistics.ShortestAvatarList, context);
            Defeat = ToAvatarDamage(roleCombatData.Detail.FightStatistics.MaxDefeatAvatar, context);
            Damage = ToAvatarDamage(roleCombatData.Detail.FightStatistics.MaxDamageAvatar, context);
            TakeDamage = ToAvatarDamage(roleCombatData.Detail.FightStatistics.MaxTakeDamageAvatar, context);
        }

        Engaged = true;
    }

    private RoleCombatView(MetadataRoleCombatSchedule roleCombatSchedule, RoleCombatMetadataContext context)
    {
        ScheduleId = roleCombatSchedule.Id;
        FormattedTime = $"{roleCombatSchedule.Begin:yyyy.MM.dd HH:mm} - {roleCombatSchedule.End:yyyy.MM.dd HH:mm}";

        Elements = roleCombatSchedule.Elements;
        SpecialAvatars = roleCombatSchedule.SpecialAvatars.SelectAsArray(static (id, context) => AvatarView.Create(context.GetAvatar(id)), context);
        InitialAvatars = roleCombatSchedule.InitialAvatars.SelectAsArray(static (id, context) => AvatarView.Create(context.GetAvatar(id)), context);
    }

    public uint ScheduleId { get; private set => SetProperty(ref field, value); }

    public string Schedule { get => SH.FormatModelEntitySpiralAbyssSchedule(ScheduleId); }

    public string FormattedTime { get; private set => SetProperty(ref field, value); }

    public bool Engaged { get; private set => SetProperty(ref field, value); }

    public RoleCombatEntry? Entity { get; private set => SetProperty(ref field, value); }

    public int TotalBattleTimes { get; private set => SetProperty(ref field, value); }

    public string? Difficulty { get; private set => SetProperty(ref field, value); }

    public string? FormattedHeraldry { get; private set => SetProperty(ref field, value); }

    public string? MaxRound
    {
        get
        {
            if (Stat is null)
            {
                return default;
            }

            return Stat.TarotFinishedCount > 0
                ? SH.FormatViewModelRoleCombatRoundAndTarot(Stat.MaxRoundId, Stat.TarotFinishedCount)
                : SH.FormatViewModelRoleCombatRound(Stat.MaxRoundId);
        }
    }

    public string FormattedTotalBattleTimes { get => $"{TimeSpan.FromSeconds(TotalBattleTimes):hh':'mm':'ss}"; }

    public RoleCombatStat? Stat { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<ElementType> Elements { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<AvatarView> SpecialAvatars { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<AvatarView> InitialAvatars { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<AvatarView> BackupAvatars { get; private set => SetProperty(ref field, value); } = [];

    public ImmutableArray<RoundView> Rounds { get; private set => SetProperty(ref field, value); } = [];

    public ImmutableArray<AvatarDamage> Shortest { get; private set => SetProperty(ref field, value); } = [];

    public AvatarDamage? Defeat { get; private set => SetProperty(ref field, value); }

    public AvatarDamage? Damage { get; private set => SetProperty(ref field, value); }

    public AvatarDamage? TakeDamage { get; private set => SetProperty(ref field, value); }

    public static RoleCombatView Create(RoleCombatEntry entity, RoleCombatMetadataContext context)
    {
        return new(entity, context);
    }

    public static RoleCombatView Create(RoleCombatEntry? entity, MetadataRoleCombatSchedule meta, RoleCombatMetadataContext context)
    {
        return entity is not null ? new(entity, meta, context) : new(meta, context);
    }

    private static ImmutableArray<AvatarDamage> ToAvatarDamages(ImmutableArray<RoleCombatAvatarStatistics> avatarDamages, RoleCombatMetadataContext context)
    {
        return avatarDamages.SelectAsArray(static (r, context) => new AvatarDamage(r.Value, context.GetAvatar(r.AvatarId)), context);
    }

    private static AvatarDamage? ToAvatarDamage(RoleCombatAvatarStatistics? avatarDamage, RoleCombatMetadataContext context)
    {
        if (avatarDamage is not { AvatarId.Value: not 0U })
        {
            return default;
        }

        return new(avatarDamage.Value, context.IdAvatarMap[avatarDamage.AvatarId]);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        switch (e.PropertyName)
        {
            case nameof(ScheduleId):
                OnPropertyChanged(nameof(Schedule));
                break;
            case nameof(TotalBattleTimes):
                OnPropertyChanged(nameof(FormattedTotalBattleTimes));
                break;
            case nameof(Stat):
                OnPropertyChanged(nameof(MaxRound));
                break;
        }
    }
}
