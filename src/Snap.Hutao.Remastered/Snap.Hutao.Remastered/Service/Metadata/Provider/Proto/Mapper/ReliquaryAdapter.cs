// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using EquipType = Snap.Hutao.Remastered.Model.Intrinsic.EquipType;
using ItemType = Snap.Hutao.Remastered.Model.Intrinsic.ItemType;
using QualityType = Snap.Hutao.Remastered.Model.Intrinsic.QualityType;
using ProtoReliquary = Snap.Hutao.Remastered.Metadata.Reliquary;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class ReliquaryAdapter : ProtoCategoryAdapter<ProtoReliquary, Model.Metadata.Reliquary.Reliquary>
{
    public ReliquaryAdapter()
        : base(MetadataCategories.Reliquary)
    {
    }

    public override IEnumerable<ProtoReliquary> Enumerate(LanguageMetadata language)
    {
        return language.Reliquaries;
    }

    public override Model.Metadata.Reliquary.Reliquary Map(ProtoReliquary message, MetadataConvert convert)
    {
        return new()
        {
            Ids = [.. message.Ids.Select(id => (ReliquaryId)id)],
            RankLevel = convert.RawEnum<QualityType>(message.RankLevel, nameof(message.RankLevel)),
            SetId = (ReliquarySetId)message.SetId,
            EquipType = convert.RawEnum<EquipType>(message.EquipType, nameof(message.EquipType)),
            ItemType = convert.RawEnum<ItemType>(message.ItemType, nameof(message.ItemType)),
            Name = convert.Text(message.Name),
            Description = convert.Text(message.Description),
            Icon = convert.Text(message.Icon),
            SortOrder = message.SortOrder,
        };
    }
}
