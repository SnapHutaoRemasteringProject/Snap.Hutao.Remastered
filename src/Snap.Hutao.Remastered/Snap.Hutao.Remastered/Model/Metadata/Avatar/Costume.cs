// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.UI.Xaml.Data;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

internal sealed partial class Costume : IPropertyValuesProvider
{
    public required CostumeId Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required bool IsDefault { get; init; }

    public string? FrontIcon { get; init; }

    public string? SideIcon { get; init; }
}