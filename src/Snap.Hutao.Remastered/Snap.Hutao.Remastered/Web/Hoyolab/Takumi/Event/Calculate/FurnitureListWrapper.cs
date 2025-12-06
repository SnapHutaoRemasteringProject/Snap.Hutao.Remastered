// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Response;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Takumi.Event.Calculate;

internal sealed class FurnitureListWrapper : ListWrapper<Item>
{
    [JsonPropertyName("not_calc_list")]
    public List<Item>? NotCalculateList { get; set; }
}