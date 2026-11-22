// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoAvatarPromote = Snap.Hutao.Remastered.Metadata.AvatarPromote;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class AvatarPromoteAdapter : ProtoCategoryAdapter<ProtoAvatarPromote, Promote>
{
    public AvatarPromoteAdapter()
        : base(MetadataCategories.AvatarPromote)
    {
    }

    public override IEnumerable<ProtoAvatarPromote> Enumerate(LanguageMetadata language)
    {
        return language.AvatarPromotes;
    }

    public override Promote Map(ProtoAvatarPromote message, MetadataConvert convert)
    {
        return new()
        {
            Id = (PromoteId)message.Id,
            Level = (PromoteLevel)message.Level,
            AddProperties = convert.FightPropertyCollection(
                message.AddProperties.Select(property => (property.Type, property.Value)),
                nameof(message.AddProperties)),
        };
    }
}
