// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using Snap.Hutao.Remastered.Model.Intrinsic;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoElementName = Snap.Hutao.Remastered.Metadata.ElementName;
using ProtoRoleCombatSchedule = Snap.Hutao.Remastered.Metadata.RoleCombatSchedule;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class RoleCombatScheduleAdapter : ProtoCategoryAdapter<ProtoRoleCombatSchedule, RoleCombatSchedule>
{
    public RoleCombatScheduleAdapter()
        : base(MetadataCategories.RoleCombatSchedule)
    {
    }

    public override IEnumerable<ProtoRoleCombatSchedule> Enumerate(LanguageMetadata language)
    {
        return language.RoleCombatSchedules;
    }

    public override RoleCombatSchedule Map(ProtoRoleCombatSchedule message, MetadataConvert convert)
    {
        return new()
        {
            Id = (RoleCombatScheduleId)message.Id,
            Begin = convert.Time(message.Begin, nameof(message.Begin)),
            End = convert.Time(message.End, nameof(message.End)),
            Elements = [.. message.Elements.Select(element => convert.ToEnum<ProtoElementName, ElementType>(element, nameof(message.Elements)))],
            SpecialAvatars = [.. message.SpecialAvatars.Select(id => (AvatarId)id)],
            InitialAvatars = [.. message.InitialAvatars.Select(id => (AvatarId)id)],
        };
    }
}
