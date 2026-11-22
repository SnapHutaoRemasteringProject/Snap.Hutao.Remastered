// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoWeaponPromote = Snap.Hutao.Remastered.Metadata.WeaponPromote;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class WeaponPromoteAdapter : ProtoCategoryAdapter<ProtoWeaponPromote, Promote>
{
    public WeaponPromoteAdapter()
        : base(MetadataCategories.WeaponPromote)
    {
    }

    public override IEnumerable<ProtoWeaponPromote> Enumerate(LanguageMetadata language)
    {
        return language.WeaponPromotes;
    }

    public override Promote Map(ProtoWeaponPromote message, MetadataConvert convert)
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
