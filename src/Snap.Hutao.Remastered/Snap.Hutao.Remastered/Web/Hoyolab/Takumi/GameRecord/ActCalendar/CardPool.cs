// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

public sealed partial class CardPool
{
    [JsonPropertyName("pool_id")]
    public required int PoolId { get; init; }

    [JsonPropertyName("version_name")]
    public required string VersionName { get; init; }

    [JsonPropertyName("pool_name")]
    public required string PoolName { get; init; }

    [JsonPropertyName("pool_type")]
    public required CardPoolType PoolType { get; init; }

    [JsonPropertyName("avatars")]
    public ImmutableArray<CardPoolAvatar> Avatars { get; init; } = [];

    [JsonPropertyName("weapon")]
    public ImmutableArray<CardPoolWeapon> Weapons { get; init; } = [];

    [JsonIgnore]
    public ImmutableArray<CardPoolItem> CompositeItems { get => !field.IsDefault ? field : field = [.. Avatars, .. Weapons]; }

    [JsonPropertyName("start_timestamp")]
    public required long StartTimestamp { get; init; }

    [JsonPropertyName("start_time")]
    public JsonElement? StartTime { get; init; }

    [JsonPropertyName("end_timestamp")]
    public required long EndTimestamp { get; init; }

    [JsonPropertyName("end_time")]
    public JsonElement? EndTime { get; init; }

    [JsonIgnore]
    public string FormattedDuration { get => SH.FormatWebHoyolabGameRecordActCalendarDuration(DateTimeOffset.FromUnixTimeSeconds(StartTimestamp).LocalDateTime, DateTimeOffset.FromUnixTimeSeconds(EndTimestamp).LocalDateTime); }

    [JsonIgnore]
    public string? FormattedStatus { get => PoolStatus.GetLocalizedDescription(SH.ResourceManager); }

    [JsonPropertyName("jump_url")]
    public required Uri JumpUrl { get; init; }

    [JsonPropertyName("pool_status")]
    public required CardPoolStatus PoolStatus { get; init; }

    [JsonPropertyName("countdown_seconds")]
    public required int CountdownSeconds { get; init; }

    [JsonIgnore]
    public string FormattedCountdown
    {
        get
        {
            TimeSpan span = TimeSpan.FromSeconds(CountdownSeconds);
            return span.Days > 0
                ? SH.FormatWebHoyolabGameRecordActCalendarCountdownDaysAndHours(span.Days, span.Hours)
                : SH.FormatWebHoyolabGameRecordActCalendarCountdownHours(span.Hours);
        }
    }
}
