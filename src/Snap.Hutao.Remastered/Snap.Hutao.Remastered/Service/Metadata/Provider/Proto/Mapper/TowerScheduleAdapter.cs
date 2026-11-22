// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Metadata;
using Snap.Hutao.Remastered.Model.Primitive;
using ProtoTowerSchedule = Snap.Hutao.Remastered.Metadata.TowerSchedule;

namespace Snap.Hutao.Remastered.Service.Metadata.Provider.Proto.Mapper;

internal sealed class TowerScheduleAdapter : ProtoCategoryAdapter<ProtoTowerSchedule, Model.Metadata.Tower.TowerSchedule>
{
    public TowerScheduleAdapter()
        : base(MetadataCategories.TowerSchedule)
    {
    }

    public override IEnumerable<ProtoTowerSchedule> Enumerate(LanguageMetadata language)
    {
        return language.TowerSchedules;
    }

    public override Model.Metadata.Tower.TowerSchedule Map(ProtoTowerSchedule message, MetadataConvert convert)
    {
        return new()
        {
            Id = (TowerScheduleId)message.Id,
            FloorIds = [.. message.FloorIds.Select(id => (TowerFloorId)id)],
            Open = convert.Time(message.Open, nameof(message.Open)),
            Close = convert.Time(message.Close, nameof(message.Close)),
            BuffName = convert.Text(message.BuffName),
            Descriptions = [.. message.Descriptions.Select(convert.Text)],
            Icon = convert.Text(message.Icon),
        };
    }
}
