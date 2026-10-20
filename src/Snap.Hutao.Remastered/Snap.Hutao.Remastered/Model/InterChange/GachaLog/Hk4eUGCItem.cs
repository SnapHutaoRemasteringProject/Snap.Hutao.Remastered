// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Core.Text.Json.Converter;
using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;

namespace Snap.Hutao.Remastered.Model.InterChange.GachaLog;

// ReSharper disable once InconsistentNaming
public sealed class Hk4eUGCItem : IJsonOnDeserialized
{
    // ReSharper disable once InconsistentNaming

    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public required long Id { get; init; }

    [JsonPropertyName("schedule_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long ScheduleId { get; init; }

    [JsonPropertyName("item_type")]
    public string? ItemType { get; init; }

    [JsonPropertyName("item_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public required uint ItemId { get; init; }

    [JsonPropertyName("item_name")]
    public string? ItemName { get; init; }

    [JsonPropertyName("rank_type")]
    public string? RankType { get; init; }

    [JsonPropertyName("time")]
    [JsonConverter(typeof(SimpleDateTimeConverter))]
    public required DateTime Time { get; init; }

    [JsonPropertyName("op_gacha_type")]
    [JsonConverter(typeof(NumberStringEnumConverter<GachaType>))]
    public required GachaType GachaType { get; init; }

    public static Hk4eUGCItem From(BeyondGachaItem item, string name, string itemType, string rankType, int timezone)
    {
        return new()
        {
            GachaType = item.GachaType,
            ItemId = item.ItemId,
            Time = item.Time.UtcDateTime.AddHours(timezone),
            ItemName = name,
            ItemType = itemType,
            RankType = rankType,
            Id = item.Id,
            ScheduleId = item.ScheduleId,
        };
    }

    public void OnDeserialized()
    {
        if (!Enum.IsDefined(GachaType))
        {
            throw new JsonException($"Unsupported GachaType: {GachaType}");
        }
    }
}
