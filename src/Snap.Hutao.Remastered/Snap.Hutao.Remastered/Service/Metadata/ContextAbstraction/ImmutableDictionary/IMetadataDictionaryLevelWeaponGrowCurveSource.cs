// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;

internal interface IMetadataDictionaryLevelWeaponGrowCurveSource
{
    ImmutableDictionary<Level, TypeValueCollection<GrowCurveType, float>> LevelDictionaryWeaponGrowCurveMap { get; set; }
}