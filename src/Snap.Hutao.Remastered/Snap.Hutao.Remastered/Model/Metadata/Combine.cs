// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class Combine
{
    [ProtoMember(1)]
    public CombineId Id { get; init; }

    [ProtoMember(2)]
    public uint Type { get; init; }

    [ProtoMember(3)]
    public uint SubType { get; init; }

    [ProtoMember(8)]
    public RecipeType RecipeType { get; init; }

    [ProtoMember(5)]
    public uint Cost { get; init; }

    [ProtoMember(4)]
    public IdCount Result { get; init; }

    [ProtoMember(6)]
    public ImmutableArray<IdCount> Materials { get; init; } = [];

    [ProtoMember(7)]
    public string EffectDescription { get; init; } = default!;
}
