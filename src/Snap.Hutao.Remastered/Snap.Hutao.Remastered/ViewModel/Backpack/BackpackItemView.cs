// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.Entity;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata.Converter;
using Snap.Hutao.Remastered.Model.Metadata.Item;
using Snap.Hutao.Remastered.Model.Metadata.Reliquary;
using Snap.Hutao.Remastered.Model.Metadata.Weapon;
using Snap.Hutao.Remastered.Service.Backpack;

namespace Snap.Hutao.Remastered.ViewModel.Backpack;

public class BackpackItemView
{
    public BackpackItem Entity { get; protected init; } = default!;

    public BackpackItemCategory Category { get; protected set; }

    public Material? Material { get; protected set; }

    public string Name { get; protected set; } = string.Empty;

    public string DisplayCount => Entity.Count > 1 ? $"x{Entity.Count}" : string.Empty;

    public string Description { get; protected set; } = string.Empty;

    public string TypeDescription { get; protected set; } = string.Empty;

    public Uri IconUri { get; protected set; } = default!;

    public QualityType Quality { get; protected set; }

    public static BackpackItemView Create(BackpackItem entity, BackpackServiceMetadataContext context)
    {
        if (context.IdWeaponMap.TryGetValue(entity.ItemId, out Weapon? weapon))
        {
            return BackpackWeaponItemView.Create(entity, context, weapon);
        }

        if (entity.MainPropId is not null && context.IdReliquaryMap.TryGetValue(entity.ItemId, out Reliquary? reliquary))
        {
            return BackpackReliquaryItemView.Create(entity, context, reliquary);
        }

        // First try to classify by ItemId alone
        // If that returns Material (default), try to get Material metadata and classify by MaterialType
        context.IdMaterialMap.TryGetValue(entity.ItemId, out Material? material);

        BackpackItemView view = new()
        {
            Entity = entity,
            Material = material,
            Category = material.GetCategory(entity.ItemId),
            Name = material?.Name ?? string.Empty,
            Description = material?.Description ?? string.Empty,
            TypeDescription = material?.TypeDescription ?? string.Empty,
            IconUri = material is not null ? ItemIconConverter.IconNameToUri(material.Icon) : default!,
            Quality = material?.RankLevel ?? QualityType.QUALITY_NONE,
        };

        return view;
    }
}
