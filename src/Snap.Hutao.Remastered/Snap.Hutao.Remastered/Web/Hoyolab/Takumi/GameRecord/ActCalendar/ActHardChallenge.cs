// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActHardChallenge : Act
{
    [JsonPropertyName("hard_challenge_detail")]
    public required ActHardChallengeDetail HardChallengeDetail { get; init; }

    [JsonIgnore]
    public override string FormattedCountdown
    {
        get
        {
            if (HardChallengeDetail.Sub.Seconds > 0)
            {
                TimeSpan span = TimeSpan.FromSeconds(HardChallengeDetail.Sub.Seconds);
                return span.Days > 0 
                    ? SH.FormatWebHoyolabGameRecordActCalendarHardChallengeSubCountdownDaysAndHours(span.Days, span.Hours)
                    : SH.FormatWebHoyolabGameRecordActCalendarHardChallengeSubCountdownHours(span.Hours);
            }

            return base.FormattedCountdown;
        }
    }
}