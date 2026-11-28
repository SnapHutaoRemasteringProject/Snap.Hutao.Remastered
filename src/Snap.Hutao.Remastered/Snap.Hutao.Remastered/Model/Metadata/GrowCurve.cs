// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS8618

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata;

[ProtoContract]
public sealed class GrowCurve
{
    [ProtoMember(2)]
    public Level Level { get; init; }

    [ProtoMember(1)]
    public TypeValueCollection<GrowCurveType, float> Curves { get; init; }
}
