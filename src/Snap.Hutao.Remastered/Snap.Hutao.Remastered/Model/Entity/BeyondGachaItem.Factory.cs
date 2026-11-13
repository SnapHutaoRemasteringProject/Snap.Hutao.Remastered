// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.InterChange.GachaLog;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;

namespace Snap.Hutao.Remastered.Model.Entity;

public sealed partial class BeyondGachaItem
{
    public static BeyondGachaItem From(Guid archiveId, BeyondGachaLogItem item)
    {
        return new()
        {
            ArchiveId = archiveId,
            GachaType = item.GachaType,
            QueryType = item.GachaType.ToQueryType(),
            ItemId = uint.Parse(item.ItemId),
            Time = item.Time,
            Id = item.Id,
            ScheduleId = long.Parse(item.ScheduleId),
            IsUp = item.IsUp,
        };
    }

    public static BeyondGachaItem From(Guid archiveId, Web.Hutao.GachaLog.GachaItem item)
    {
        return new()
        {
            ArchiveId = archiveId,
            GachaType = item.GachaType,
            QueryType = item.QueryType,
            ItemId = item.ItemId,
            Time = item.Time,
            Id = item.Id,
            ScheduleId = 0,
            IsUp = 0,
        };
    }
    public static BeyondGachaItem From(Guid archiveId, Hk4eUGCItem item, int timezoneOffset)
    {
        return new()
        {
            ArchiveId = archiveId,
            GachaType = item.GachaType,
            QueryType = item.GachaType.ToQueryType(),
            ItemId = item.ItemId,
            Time = new(item.Time, TimeSpan.FromHours(timezoneOffset)),
            Id = item.Id,
            ScheduleId = item.ScheduleId,
            IsUp = 0, // Hk4eUGCItem doesn't have IsUp
        };
    }
}
