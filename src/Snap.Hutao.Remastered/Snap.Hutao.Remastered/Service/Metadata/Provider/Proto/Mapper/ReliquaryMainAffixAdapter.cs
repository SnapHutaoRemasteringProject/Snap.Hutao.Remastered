// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using FightProperty = Snap.Hutao.Remastered.Model.Intrinsic.FightProperty;
using ProtoReliquaryMainAffix = Snap.Hutao.Remastered.Metadata.ReliquaryMainAffix;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class ReliquaryMainAffixAdapter : ProtoCategoryAdapter<ProtoReliquaryMainAffix, Model.Metadata.Reliquary.ReliquaryMainAffix>
{
    public ReliquaryMainAffixAdapter()
        : base(MetadataCategories.ReliquaryMainAffix)
    {
    }

    public override IEnumerable<ProtoReliquaryMainAffix> Enumerate(LanguageMetadata language)
    {
        return language.ReliquaryMainAffixes;
    }

    public override Model.Metadata.Reliquary.ReliquaryMainAffix Map(ProtoReliquaryMainAffix message, MetadataConvert convert)
    {
        return new()
        {
            Id = (ReliquaryMainAffixId)message.Id,
            Type = convert.RawEnum<FightProperty>(message.Type, nameof(message.Type)),
        };
    }
}
