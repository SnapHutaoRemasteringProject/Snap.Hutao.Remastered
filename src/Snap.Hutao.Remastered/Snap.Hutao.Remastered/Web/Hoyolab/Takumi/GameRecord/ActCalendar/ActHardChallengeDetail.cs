// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActHardChallengeDetail
{
    [JsonPropertyName("is_unlock")]
    public required bool IsUnlock { get; init; }

    [JsonPropertyName("difficulty")]
    public required HardChallengeDifficultyLevel Difficulty { get; init; }

    [JsonPropertyName("second")]
    public required int Second { get; init; }

    [JsonPropertyName("icon")]
    public required Uri Icon { get; init; }

    [JsonPropertyName("sub")]
    public required ActHardChallengeDetailSub Sub { get; init; }

    [JsonIgnore]
    public string FormattedProgress
    {
        get
        {
            if (!IsUnlock)
            {
                return SH.WebHoyolabGameRecordActCalendarHardChallengeLocked;
            }

            return $"{Sub.X}/{Sub.Y}";
        }
    }
}