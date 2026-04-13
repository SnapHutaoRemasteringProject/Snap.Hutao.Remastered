// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.ActCalendar;

internal sealed class ActLiBen : Act
{
    [JsonPropertyName("liben_detail")]
    public required ActLiBenDetail LiBenDetail { get; init; }
}