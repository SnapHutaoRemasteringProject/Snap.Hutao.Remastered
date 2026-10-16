// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.RoleCombat;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.RoleCombat;

public sealed partial class RoundView : ObservableObject
{
    private RoundView(RoleCombatRoundData data, in TimeSpan offset, RoleCombatMetadataContext context)
    {
        FinishTime = DateTimeOffset.FromUnixTimeSeconds(data.FinishTime).ToOffset(offset);

        RoundId = data.IsTarot
            ? SH.FormatViewModelRoleCombatTarot(data.TarotSerialNumber.ToRoman())
            : SH.FormatViewModelRoleCombatRound(data.RoundId);
        IsGetMedal = data.IsGetMedal;
        IsTarot = data.IsTarot;
        FinishTimeString = $"{FinishTime:yyyy.MM.dd HH:mm:ss}";
        Enemies = data.Enemies.SelectAsArray(EnemyView.Create);
        Avatars = data.Avatars.SelectAsArray(AvatarView.Create, context);
        ChoiceCards = data.ChoiceCards.SelectAsArray(BuffView.Create);
        SplendourSummary = data.SplendourBuff.Summary.Description.Replace("\\n", "\n", StringComparison.OrdinalIgnoreCase);
        SplendourBuffs = data.SplendourBuff.Buffs.SelectAsArray(SplendourBuffView.Create);
    }

    public string RoundId { get; private set => SetProperty(ref field, value); }

    public bool IsGetMedal { get; private set => SetProperty(ref field, value); }

    public bool IsTarot { get; private set => SetProperty(ref field, value); }

    public string FinishTimeString { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<EnemyView> Enemies { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<AvatarView> Avatars { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<BuffView> ChoiceCards { get; private set => SetProperty(ref field, value); }

    public string SplendourSummary { get; private set => SetProperty(ref field, value); }

    public ImmutableArray<SplendourBuffView> SplendourBuffs { get; private set => SetProperty(ref field, value); }

    public DateTimeOffset FinishTime { get; private set => SetProperty(ref field, value); }

    public static RoundView Create(RoleCombatRoundData data, TimeSpan offset, RoleCombatMetadataContext context)
    {
        return new(data, offset, context);
    }
}
