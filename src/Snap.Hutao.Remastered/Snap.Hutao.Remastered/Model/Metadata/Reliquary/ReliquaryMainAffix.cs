// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata.Reliquary;

[ProtoContract]
public class ReliquaryMainAffix
{
    [ProtoMember(1)]
    public ReliquaryMainAffixId Id { get; init; }

    [ProtoMember(2)]
    public FightProperty Type { get; init; }
}
