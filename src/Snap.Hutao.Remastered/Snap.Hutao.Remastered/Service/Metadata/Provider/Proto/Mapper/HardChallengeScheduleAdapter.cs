// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using LanguageMetadata = Snap.Hutao.Remastered.Metadata.LanguageMetadata;
using Snap.Hutao.Remastered.Model.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoHardChallengeSchedule = Snap.Hutao.Remastered.Metadata.HardChallengeSchedule;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class HardChallengeScheduleAdapter : ProtoCategoryAdapter<ProtoHardChallengeSchedule, HardChallengeSchedule>
{
    public HardChallengeScheduleAdapter()
        : base(MetadataCategories.HardChallengeSchedule)
    {
    }

    public override IEnumerable<ProtoHardChallengeSchedule> Enumerate(LanguageMetadata language)
    {
        return language.HardChallengeSchedules;
    }

    public override HardChallengeSchedule Map(ProtoHardChallengeSchedule message, MetadataConvert convert)
    {
        return new()
        {
            Id = (HardChallengeScheduleId)message.Id,
            Begin = convert.Time(message.Begin, nameof(message.Begin)),
            End = convert.Time(message.End, nameof(message.End)),
            Name = convert.Text(message.Name),
        };
    }
}
