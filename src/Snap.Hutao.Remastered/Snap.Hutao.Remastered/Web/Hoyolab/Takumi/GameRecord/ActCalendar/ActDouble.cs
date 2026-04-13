// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActDouble : Act
{
    [JsonPropertyName("double_detail")]
    public required ActDoubleDetail DoubleDetail { get; init; }
}