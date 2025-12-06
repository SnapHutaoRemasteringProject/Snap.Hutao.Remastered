// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Enka.Model;

internal sealed class ShowAvatarInfo
{
    [JsonPropertyName("avatarId")]
    public AvatarId AvatarId { get; set; }

    [JsonPropertyName("level")]
    public Level Level { get; set; }

    [JsonPropertyName("energyType")]
    public ElementType EnergyType { get; set; }

    [JsonPropertyName("costumeId")]
    public CostumeId? CostumeId { get; set; }
}