// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoDisplayItem = Snap.Hutao.Remastered.Metadata.DisplayItem;
using ItemType = Snap.Hutao.Remastered.Model.Intrinsic.ItemType;
using QualityType = Snap.Hutao.Remastered.Model.Intrinsic.QualityType;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class DisplayItemAdapter : ProtoCategoryAdapter<ProtoDisplayItem, Model.Metadata.Item.DisplayItem>
{
    public DisplayItemAdapter()
        : base(MetadataCategories.DisplayItem)
    {
    }

    public override IEnumerable<ProtoDisplayItem> Enumerate(LanguageMetadata language)
    {
        return language.DisplayItems;
    }

    public override Model.Metadata.Item.DisplayItem Map(ProtoDisplayItem message, MetadataConvert convert)
    {
        return new()
        {
            Id = (MaterialId)message.Id,
            RankLevel = convert.RawEnum<QualityType>(message.RankLevel, nameof(message.RankLevel)),
            ItemType = convert.RawEnum<ItemType>(message.ItemType, nameof(message.ItemType)),
            Rank = message.Rank,
            Name = convert.Text(message.Name),
            Description = convert.OptionalText(message.Description),
            TypeDescription = convert.OptionalText(message.TypeDescription),
            Icon = convert.Text(message.Icon),
        };
    }
}
