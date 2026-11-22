// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoItemType = Snap.Hutao.Remastered.Metadata.ItemType;
using ItemType = Snap.Hutao.Remastered.Model.Intrinsic.ItemType;
using MaterialType = Snap.Hutao.Remastered.Model.Intrinsic.MaterialType;
using QualityType = Snap.Hutao.Remastered.Model.Intrinsic.QualityType;
using ProtoMaterial = Snap.Hutao.Remastered.Metadata.Material;
using ProtoMaterialType = Snap.Hutao.Remastered.Metadata.MaterialType;
using ProtoQualityType = Snap.Hutao.Remastered.Metadata.QualityType;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class MaterialAdapter : ProtoCategoryAdapter<ProtoMaterial, Model.Metadata.Item.Material>
{
    public MaterialAdapter()
        : base(MetadataCategories.Material)
    {
    }

    public override IEnumerable<ProtoMaterial> Enumerate(LanguageMetadata language)
    {
        return language.Materials;
    }

    public override Model.Metadata.Item.Material Map(ProtoMaterial message, MetadataConvert convert)
    {
        return new()
        {
            Id = (MaterialId)message.Id,
            Rank = message.Rank,
            MaterialType = convert.ToEnum<ProtoMaterialType, MaterialType>(message.MaterialType, nameof(message.MaterialType)),
            EffectDescription = convert.OptionalText(message.EffectDescription),
            RankLevel = convert.ToEnum<ProtoQualityType, QualityType>(message.RankLevel, nameof(message.RankLevel)),
            ItemType = convert.ToEnum<ProtoItemType, ItemType>(message.ItemType, nameof(message.ItemType)),
            Name = convert.Text(message.Name),
            Description = convert.OptionalText(message.Description),
            TypeDescription = convert.OptionalText(message.TypeDescription),
            Icon = convert.Text(message.Icon),
        };
    }
}
