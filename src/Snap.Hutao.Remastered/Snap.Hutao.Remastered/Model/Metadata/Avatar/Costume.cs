// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.UI.Xaml.Data;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
public sealed partial class Costume : ObservableObject, IPropertyValuesProvider
{
    [ProtoMember(1)]
    public CostumeId Id { get; init => SetProperty(ref field, value); }

    [ProtoMember(2)]
    public string Name { get; init => SetProperty(ref field, value); }

    [ProtoMember(3)]
    public string Description { get; init => SetProperty(ref field, value); }

    [ProtoMember(4)]
    public bool IsDefault { get; init => SetProperty(ref field, value); }

    [ProtoMember(5)]
    public string? FrontIcon { get; init => SetProperty(ref field, value); }

    [ProtoMember(6)]
    public string? SideIcon { get; init => SetProperty(ref field, value); }
}
