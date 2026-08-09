// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Metadata.Reliquary;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Model.Primitive;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableArray;
using Snap.Hutao.Remastered.Service.Metadata.ContextAbstraction.ImmutableDictionary;
using System.Collections.Immutable;

namespace Snap.Hutao.Remastered.Service.Backpack;

public sealed class BackpackServiceMetadataContext : IMetadataContext,
    IMetadataDictionaryIdDisplayItemAndMaterialSource,
    IMetadataDictionaryIdMaterialSource,
    IMetadataDictionaryIdWeaponSource,
    IMetadataDictionaryIdReliquarySource,
    IMetadataDictionaryIdReliquarySetSource,
    IMetadataDictionaryIdReliquaryMainPropertySource,
    IMetadataDictionaryIdReliquarySubAffixSource,
    IMetadataArrayReliquaryMainAffixLevelSource,
    IMetadataArrayCookRecipeSource,
    IMetadataDictionaryIdCookRecipeSource
{
    public ImmutableDictionary<MaterialId, DisplayItem> IdDisplayItemAndMaterialMap { get; set; } = default!;

    public ImmutableDictionary<MaterialId, Material> IdMaterialMap { get; set; } = default!;

    public ImmutableDictionary<WeaponId, Weapon> IdWeaponMap { get; set; } = default!;

    public ImmutableDictionary<ReliquaryId, Reliquary> IdReliquaryMap { get; set; } = default!;

    public ImmutableDictionary<ReliquarySetId, ReliquarySet> IdReliquarySetMap { get; set; } = default!;

    public ImmutableDictionary<ReliquaryMainAffixId, FightProperty> IdReliquaryMainPropertyMap { get; set; } = default!;

    public ImmutableDictionary<ReliquarySubAffixId, ReliquarySubAffix> IdReliquarySubAffixMap { get; set; } = default!;

    public ImmutableArray<ReliquaryMainAffixLevel> ReliquaryMainAffixLevels { get; set; }

    public ImmutableArray<CookRecipe> CookRecipes { get; set; }

    public ImmutableDictionary<CookRecipeId, CookRecipe> IdCookRecipeMap { get; set; } = default!;

    public BackpackReliquaryScoreConfig ReliquaryScoreConfig { get; set; } = default!;
}
