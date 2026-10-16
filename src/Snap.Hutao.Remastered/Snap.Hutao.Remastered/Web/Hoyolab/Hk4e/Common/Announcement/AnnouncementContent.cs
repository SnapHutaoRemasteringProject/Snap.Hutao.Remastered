// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Common.Announcement;

public partial class AnnouncementContent : ObservableObject
{
    [JsonPropertyName("ann_id")]
    [ObservableProperty]
    public partial int AnnId { get; set; }

    [JsonPropertyName("title")]
    [ObservableProperty]
    public partial string Title { get; set; } = default!;

    [JsonPropertyName("subtitle")]
    [ObservableProperty]
    public partial string Subtitle { get; set; } = default!;

    [JsonPropertyName("banner")]
    [ObservableProperty]
    public partial string Banner { get; set; } = default!;

    [JsonPropertyName("content")]
    [ObservableProperty]
    public partial string Content { get; set; } = default!;

    [JsonPropertyName("lang")]
    [ObservableProperty]
    public partial string Lang { get; set; } = default!;
}
