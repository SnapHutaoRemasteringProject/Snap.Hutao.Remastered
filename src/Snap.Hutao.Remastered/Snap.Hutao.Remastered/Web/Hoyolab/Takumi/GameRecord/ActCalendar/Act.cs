// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ActSignIn), nameof(ActType.ActTypeSignIn))]
[JsonDerivedType(typeof(ActDouble), nameof(ActType.ActTypeDouble))]
[JsonDerivedType(typeof(ActExplore), nameof(ActType.ActTypeExplore))]
[JsonDerivedType(typeof(ActLiBen), nameof(ActType.ActTypeLiBen))]
[JsonDerivedType(typeof(ActTower), nameof(ActType.ActTypeTower))]
[JsonDerivedType(typeof(ActRoleCombat), nameof(ActType.ActTypeRoleCombat))]
[JsonDerivedType(typeof(ActHardChallenge), nameof(ActType.ActTypeHardChallenge))]
[JsonDerivedType(typeof(ActOther), nameof(ActType.ActTypeOther))]
internal class Act
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("start_timestamp")]
    public required long StartTimestamp { get; init; }

    [JsonPropertyName("start_time")]
    public JsonElement? StartTime { get; init; }

    [JsonPropertyName("end_timestamp")]
    public required long EndTimestamp { get; init; }

    [JsonPropertyName("end_time")]
    public JsonElement? EndTime { get; init; }

    [JsonIgnore]
    public string FormattedDuration { get => $"{DateTimeOffset.FromUnixTimeSeconds(StartTimestamp).LocalDateTime:yyyy/MM/dd HH:mm} - {DateTimeOffset.FromUnixTimeSeconds(EndTimestamp).LocalDateTime:yyyy/MM/dd HH:mm}"; }

    [JsonPropertyName("desc")]
    public required string Description { get; init; }

    [JsonPropertyName("strategy")]
    public required Uri Strategy { get; init; }

    [JsonPropertyName("countdown_seconds")]
    public required int CountdownSeconds { get; init; }

    [JsonIgnore]
    public virtual string FormattedCountdown
    {
        get
        {
            TimeSpan span = TimeSpan.FromSeconds(CountdownSeconds);
            return span.Days > 0
                ? SH.FormatWebHoyolabGameRecordActCalendarCountdownDaysAndHours(span.Days, span.Hours)
                : SH.FormatWebHoyolabGameRecordActCalendarCountdownHours(span.Hours);
        }
    }

    [JsonPropertyName("status")]
    public required ActStatus Status { get; init; }

    [JsonIgnore]
    public string? FormattedStatus { get => Status.GetLocalizedDescription(SH.ResourceManager); }

    [JsonPropertyName("reward_list")]
    public required ImmutableArray<ActReward> RewardList { get; init; }

    [JsonPropertyName("is_finished")]
    public required bool IsFinished { get; set; }
}