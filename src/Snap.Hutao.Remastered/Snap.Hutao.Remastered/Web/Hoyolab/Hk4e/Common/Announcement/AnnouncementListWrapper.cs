// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Web.Response;

namespace Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Common.Announcement;

[ObservableObject]
public sealed partial class AnnouncementListWrapper : ListWrapper<Announcement>
{
    [JsonPropertyName("list")]
    public new System.Collections.Immutable.ImmutableArray<Announcement> List
    {
        get => base.List.EmptyIfDefault();
        set
        {
            base.List = value.EmptyIfDefault();
            OnPropertyChanged();
        }
    }

    [JsonPropertyName("type_id")]
    [ObservableProperty]
    public partial int TypeId { get; set; }

    [JsonPropertyName("type_label")]
    [ObservableProperty]
    public partial string TypeLabel { get; set; } = default!;
}
