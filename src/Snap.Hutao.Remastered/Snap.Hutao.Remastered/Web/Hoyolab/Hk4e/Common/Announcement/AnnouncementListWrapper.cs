// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Web.Response;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Common.Announcement;

internal sealed class AnnouncementListWrapper : ListWrapper<Announcement>
{
    [JsonPropertyName("type_id")]
    public int TypeId { get; set; }

    [JsonPropertyName("type_label")]
    public string TypeLabel { get; set; } = default!;
}
