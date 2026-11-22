// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoReliquarySet = Snap.Hutao.Remastered.Metadata.ReliquarySet;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class ReliquarySetAdapter : ProtoCategoryAdapter<ProtoReliquarySet, Model.Metadata.Reliquary.ReliquarySet>
{
    public ReliquarySetAdapter()
        : base(MetadataCategories.ReliquarySet)
    {
    }

    public override IEnumerable<ProtoReliquarySet> Enumerate(LanguageMetadata language)
    {
        return language.ReliquarySets;
    }

    public override Model.Metadata.Reliquary.ReliquarySet Map(ProtoReliquarySet message, MetadataConvert convert)
    {
        return new()
        {
            SetId = (ReliquarySetId)message.SetId,
            EquipAffixId = (EquipAffixId)message.EquipAffixId,
            EquipAffixIds = [.. message.EquipAffixIds.Select(id => (ExtendedEquipAffixId)id)],
            Name = convert.Text(message.Name),
            Icon = convert.Text(message.Icon),
            NeedNumber = [.. message.NeedNumber.Select(value => (int)value)],
            Descriptions = [.. message.Descriptions.Select(convert.Text)],
        };
    }
}
