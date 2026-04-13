// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class CardPoolAvatar : CardPoolItem
{
    [JsonPropertyName("id")]
    public required AvatarId Id { get; set; }

    [JsonPropertyName("element")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ElementName Element { get; init; }

    [JsonPropertyName("is_invisible")]
    public required bool IsInvisible { get; set; }
}