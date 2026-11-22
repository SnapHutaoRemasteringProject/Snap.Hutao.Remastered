// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using FightProperty = Snap.Hutao.Remastered.Model.Intrinsic.FightProperty;
using ProtoReliquarySubAffix = Snap.Hutao.Remastered.Metadata.ReliquarySubAffix;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class ReliquarySubAffixAdapter : ProtoCategoryAdapter<ProtoReliquarySubAffix, Model.Metadata.Reliquary.ReliquarySubAffix>
{
    public ReliquarySubAffixAdapter()
        : base(MetadataCategories.ReliquarySubAffix)
    {
    }

    public override IEnumerable<ProtoReliquarySubAffix> Enumerate(LanguageMetadata language)
    {
        return language.ReliquarySubAffixes;
    }

    public override Model.Metadata.Reliquary.ReliquarySubAffix Map(ProtoReliquarySubAffix message, MetadataConvert convert)
    {
        return new()
        {
            Id = (ReliquarySubAffixId)message.Id,
            Type = convert.RawEnum<FightProperty>(message.Type, nameof(message.Type)),
            Value = convert.Single(message.Value),
        };
    }
}
