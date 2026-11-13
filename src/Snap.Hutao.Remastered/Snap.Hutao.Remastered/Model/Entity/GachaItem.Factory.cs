// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Remastered.Model.InterChange.GachaLog;
using Snap.Hutao.Remastered.Web.Hoyolab.Hk4e.Event.GachaInfo;

namespace Snap.Hutao.Remastered.Model.Entity;

public sealed partial class GachaItem
{
    public static GachaItem From(Guid archiveId, GachaLogItem item, uint itemId)
    {
        return new()
        {
            ArchiveId = archiveId,
            GachaType = item.GachaType,
            QueryType = item.GachaType.ToQueryType(),
            ItemId = itemId,
            Time = item.Time,
            Id = item.Id,
        };
    }

    public static GachaItem From(Guid archiveId, Web.Hutao.GachaLog.GachaItem item)
    {
        return new()
        {
            ArchiveId = archiveId,
            GachaType = item.GachaType,
            QueryType = item.QueryType,
            ItemId = item.ItemId,
            Time = item.Time,
            Id = item.Id,
        };
    }

    public static GachaItem From(Guid archiveId, Hk4eItem item, int timezoneOffset)
    {
        return new()
        {
            ArchiveId = archiveId,
            GachaType = item.GachaType,
            QueryType = (GachaType)item.UIGFGachaType,
            ItemId = item.ItemId,
            Time = new(item.Time, TimeSpan.FromHours(timezoneOffset)),
            Id = item.Id,
        };
    }
}
