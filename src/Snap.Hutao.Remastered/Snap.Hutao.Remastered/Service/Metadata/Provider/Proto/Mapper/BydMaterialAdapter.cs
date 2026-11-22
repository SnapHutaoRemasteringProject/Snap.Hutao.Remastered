// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoBydMaterial = Snap.Hutao.Remastered.Metadata.BydMaterial;
using QualityType = Snap.Hutao.Remastered.Model.Intrinsic.QualityType;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class BydMaterialAdapter : ProtoCategoryAdapter<ProtoBydMaterial, Model.Metadata.Item.BydMaterial>
{
    public BydMaterialAdapter()
        : base(MetadataCategories.BydMaterial)
    {
    }

    public override IEnumerable<ProtoBydMaterial> Enumerate(LanguageMetadata language)
    {
        return language.BydMaterials;
    }

    public override Model.Metadata.Item.BydMaterial Map(ProtoBydMaterial message, MetadataConvert convert)
    {
        return new()
        {
            Id = (BydMaterialId)message.Id,
            Name = convert.Text(message.Name),
            Description = convert.OptionalText(message.Description),
            Icon = convert.Text(message.Icon),
            Type = (int)message.Type,
            RankLevel = convert.RawEnum<QualityType>(message.RankLevel, nameof(message.RankLevel)),
        };
    }
}
