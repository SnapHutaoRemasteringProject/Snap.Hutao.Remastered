// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

internal sealed class CookBonus
{
    public required MaterialId OriginItemId { get; init; }

    public required MaterialId ItemId { get; init; }

    public required ImmutableArray<MaterialId> InputList { get; init; }
}