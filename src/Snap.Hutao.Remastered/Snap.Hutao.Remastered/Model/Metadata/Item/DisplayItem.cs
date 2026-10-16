// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Item;

public partial class DisplayItem : ObservableObject, IDefaultIdentity<MaterialId>
{
    public required MaterialId Id { get; init => SetProperty(ref field, value); }

    public required QualityType RankLevel { get; init => SetProperty(ref field, value); }

    public required ItemType ItemType { get; init => SetProperty(ref field, value); }

    public required string Icon { get; init => SetProperty(ref field, value); }

    public required string Name { get; init => SetProperty(ref field, value); }

    public uint Rank { get; init => SetProperty(ref field, value); }

    public string? Description { get; init => SetProperty(ref field, value); }

    public string? TypeDescription { get; init => SetProperty(ref field, value); }
}
