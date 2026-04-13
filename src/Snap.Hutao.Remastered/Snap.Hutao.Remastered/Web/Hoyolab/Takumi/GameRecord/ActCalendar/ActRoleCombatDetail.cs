// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActRoleCombatDetail
{
    [JsonPropertyName("is_unlock")]
    public required bool IsUnlock { get; init; }

    [JsonPropertyName("max_round_id")]
    public required int MaxRoundId { get; init; }

    [JsonIgnore]
    public string FormattedMaxRound
    {
        get
        {
            if (!IsUnlock)
            {
                return SH.WebHoyolabGameRecordActCalendarRoleCombatLocked;
            }

            if (MaxRoundId <= 0)
            {
                return SH.WebHoyolabGameRecordActCalendarRoleCombatMaxRoundZero;
            }

            return SH.FormatViewModelRoleCombatRound(MaxRoundId);
        }
    }

    [JsonPropertyName("has_data")]
    public required bool HasData { get; init; }

    [JsonPropertyName("tarot_finished_cnt")]
    public required int TarotFinishedCount { get; init; }

    [JsonPropertyName("difficulty_id")]
    public required RoleCombatDifficultyLevel DifficultyId { get; init; }
}