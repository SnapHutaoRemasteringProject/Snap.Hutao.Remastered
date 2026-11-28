// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Item;

[ProtoContract]
[ProtoInclude(100, typeof(Material))]
public partial class DisplayItem : ObservableObject, IDefaultIdentity<MaterialId>
{
    [ProtoMember(1)]
    public MaterialId Id { get; init => SetProperty(ref field, value); }

    [ProtoMember(2)]
    public QualityType RankLevel { get; init => SetProperty(ref field, value); }

    [ProtoMember(4)]
    public ItemType ItemType { get; init => SetProperty(ref field, value); }

    [ProtoMember(8)]
    public string Icon { get; init => SetProperty(ref field, value); }

    [ProtoMember(5)]
    public string Name { get; init => SetProperty(ref field, value); }

    [ProtoMember(3)]
    public uint Rank { get; init => SetProperty(ref field, value); }

    [ProtoMember(6)]
    public string? Description { get; init => SetProperty(ref field, value); }

    [ProtoMember(7)]
    public string? TypeDescription { get; init => SetProperty(ref field, value); }
}
