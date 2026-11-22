// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoHyperLinkName = Snap.Hutao.Remastered.Metadata.HyperLinkName;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class HyperLinkNameAdapter : ProtoCategoryAdapter<ProtoHyperLinkName, HyperLinkName>
{
    public HyperLinkNameAdapter()
        : base(MetadataCategories.HyperLinkName)
    {
    }

    public override IEnumerable<ProtoHyperLinkName> Enumerate(LanguageMetadata language)
    {
        return language.HyperLinkNames;
    }

    public override HyperLinkName Map(ProtoHyperLinkName message, MetadataConvert convert)
    {
        return new()
        {
            Id = (HyperLinkNameId)message.Id,
            Name = convert.Text(message.Name),
            Description = convert.Text(message.Description),
        };
    }
}
