// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Wiki;

internal sealed class BaseValueInfoMetadataContext
{
    public required ImmutableDictionary<Level, TypeValueCollection<GrowCurveType, float>> GrowCurveMap { get; set; }

    public required ImmutableDictionary<PromoteLevel, Promote>? PromoteMap { get; set; }
}