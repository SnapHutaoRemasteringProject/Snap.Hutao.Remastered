// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class Promote
{
    [ProtoMember(1)]
    public PromoteId Id { get; init; }

    [ProtoMember(2)]
    public PromoteLevel Level { get; init; }

    [ProtoMember(3)]
    public TypeValueCollection<FightProperty, float> AddProperties { get; init; }
}
