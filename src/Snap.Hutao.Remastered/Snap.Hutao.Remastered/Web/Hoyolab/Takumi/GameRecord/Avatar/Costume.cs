// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.GameRecord.Avatar;

internal sealed class Costume
{
    [JsonPropertyName("id")]
    public CostumeId Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    // Ignored field string icon
}