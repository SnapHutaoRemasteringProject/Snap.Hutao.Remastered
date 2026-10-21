using Snap.Hutao.Remastered.Core.Text.Json.Annotation;
using Snap.Hutao.Remastered.Core.Text.Json.Converter;
using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;

public class BeyondGachaLogItem
{
    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; } = default!;

    [JsonPropertyName("region")]
    [JsonConverter(typeof(RegionConverter))]
    public Region Region { get; set; } = default!;

    [JsonPropertyName("uid")]
    public string Uid { get; set; } = default!;

    [JsonPropertyName("schedule_id")]
    public string ScheduleId { get; set; } = default!;

    [JsonPropertyName("item_type")]
    public string ItemType { get; set; } = default!;

    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = default!;

    [JsonPropertyName("item_name")]
    public string ItemName { get; set; } = default!;

    [JsonPropertyName("rank_type")]
    [JsonConverter(typeof(NumberStringEnumConverter<QualityType>))]
    public QualityType RankType { get; set; } = default!;

    [JsonPropertyName("is_up")]
    [JsonEnumHandling(JsonEnumHandling.NumberString)]
    public int IsUp { get; set; } = default!;

    [JsonPropertyName("time")]
    [JsonConverter(typeof(SimpleDateTimeOffsetConverter))]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("op_gacha_type")]
    [JsonConverter(typeof(NumberStringEnumConverter<GachaType>))]
    public GachaType GachaType { get; set; } = default!;
}
