// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;

namespace Snap.Hutao.Remastered.Model.Metadata.Reliquary;

[ProtoContract]
public sealed class ReliquaryMainAffixLevel
{
    [ProtoMember(1)]
    public QualityType Rank { get; init; }

    [ProtoMember(2)]
    public uint Level { get; init; }

    [ProtoMember(3)]
    public TypeValueCollection<FightProperty, float> Properties { get; init; }
}
