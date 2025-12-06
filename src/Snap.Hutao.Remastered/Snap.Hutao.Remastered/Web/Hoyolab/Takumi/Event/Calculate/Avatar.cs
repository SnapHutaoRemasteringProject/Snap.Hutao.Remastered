// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Event.Calculate;

internal sealed class Avatar : Calculable
{
    [JsonPropertyName("avatar_level")]
    public QualityType AvatarLevel { get; set; }

    [JsonPropertyName("weapon_cat_id")]
    public WeaponType WeaponCatId { get; set; }

    [JsonPropertyName("element_attr_id")]
    public ElementAttributeId ElementAttrId { get; set; }

    [JsonPropertyName("promote_level")]
    public PromoteLevel PromoteLevel { get; set; }
}