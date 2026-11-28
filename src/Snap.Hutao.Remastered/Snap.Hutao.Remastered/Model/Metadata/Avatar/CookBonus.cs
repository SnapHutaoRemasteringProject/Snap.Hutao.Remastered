// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata.Avatar;

[ProtoContract]
public sealed class CookBonus
{
    [ProtoMember(1)]
    public MaterialId OriginItemId { get; init; }

    [ProtoMember(2)]
    public MaterialId ItemId { get; init; }

    [ProtoMember(3)]
    public ImmutableArray<MaterialId> InputList { get; init; } = [];
}
