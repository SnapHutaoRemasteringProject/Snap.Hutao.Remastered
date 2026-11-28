// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Reliquary;

[ProtoContract]
public sealed class ReliquarySubAffix : IDefaultIdentity<ReliquarySubAffixId>
{
    [ProtoMember(2)]
    public ReliquarySubAffixId Id { get; init; }

    [ProtoMember(3)]
    public FightProperty Type { get; init; }

    [ProtoMember(1)]
    public float Value { get; init; }
}
