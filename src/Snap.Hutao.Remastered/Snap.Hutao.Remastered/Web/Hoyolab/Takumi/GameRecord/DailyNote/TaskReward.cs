// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.DailyNote;

public sealed class TaskReward
{
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter<TaskRewardStatus>))]
    public TaskRewardStatus Status { get; set; }
}
