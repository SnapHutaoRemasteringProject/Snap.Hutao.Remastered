// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Primitive;

namespace Snap.Hutao.Remastered.Model.Metadata;

internal sealed class GrowCurve
{
    public required Level Level { get; init; }

    public required TypeValueCollection<GrowCurveType, float> Curves { get; init; }
}