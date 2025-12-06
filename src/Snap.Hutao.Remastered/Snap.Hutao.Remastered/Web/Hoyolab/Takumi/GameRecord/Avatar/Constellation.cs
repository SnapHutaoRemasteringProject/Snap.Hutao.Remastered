// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar;

internal sealed class Constellation
{
    [JsonPropertyName("id")]
    public SkillId Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = default!;

    [JsonPropertyName("effect")]
    public string Effect { get; set; } = default!;

    [JsonPropertyName("is_actived")]
    public bool IsActived { get; set; }

    [JsonPropertyName("pos")]
    public int Position { get; set; }
}