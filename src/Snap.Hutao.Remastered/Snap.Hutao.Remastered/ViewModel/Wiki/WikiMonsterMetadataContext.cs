// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Metadata.Monster;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableArray;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.ViewModel.Wiki;

internal sealed class WikiMonsterMetadataContext : IMetadataContext,
    IMetadataDictionaryLevelMonsterGrowCurveSource,
    IMetadataArrayMonsterSource,
    IMetadataDictionaryIdDisplayItemAndMaterialSource
{
    public ImmutableDictionary<Level, TypeValueCollection<GrowCurveType, float>> LevelDictionaryMonsterGrowCurveMap { get; set; } = default!;

    public ImmutableArray<Monster> Monsters { get; set; } = default!;

    public ImmutableDictionary<MaterialId, DisplayItem> IdDisplayItemAndMaterialMap { get; set; } = default!;
}