// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using QualityType = Snap.Hutao.Remastered.Model.Intrinsic.QualityType;
using ProtoReliquaryMainAffixLevel = Snap.Hutao.Remastered.Metadata.ReliquaryMainAffixLevel;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class ReliquaryMainAffixLevelAdapter : ProtoCategoryAdapter<ProtoReliquaryMainAffixLevel, Model.Metadata.Reliquary.ReliquaryMainAffixLevel>
{
    public ReliquaryMainAffixLevelAdapter()
        : base(MetadataCategories.ReliquaryMainAffixLevel)
    {
    }

    public override IEnumerable<ProtoReliquaryMainAffixLevel> Enumerate(LanguageMetadata language)
    {
        return language.ReliquaryMainAffixLevels;
    }

    public override Model.Metadata.Reliquary.ReliquaryMainAffixLevel Map(ProtoReliquaryMainAffixLevel message, MetadataConvert convert)
    {
        return new()
        {
            Rank = convert.RawEnum<QualityType>(message.Rank, nameof(message.Rank)),
            Level = message.Level,
            Properties = convert.RawFightPropertyCollection(
                message.Properties.Select(property => (property.Type, property.Value)),
                nameof(message.Properties)),
        };
    }
}
