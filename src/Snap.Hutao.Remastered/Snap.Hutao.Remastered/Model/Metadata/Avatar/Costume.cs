// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.UI.Xaml.Data;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

public sealed partial class Costume : ObservableObject, IPropertyValuesProvider
{
    public required CostumeId Id { get; init => SetProperty(ref field, value); }

    public required string Name { get; init => SetProperty(ref field, value); }

    public required string Description { get; init => SetProperty(ref field, value); }

    public required bool IsDefault { get; init => SetProperty(ref field, value); }

    public string? FrontIcon { get; init => SetProperty(ref field, value); }

    public string? SideIcon { get; init => SetProperty(ref field, value); }
}
